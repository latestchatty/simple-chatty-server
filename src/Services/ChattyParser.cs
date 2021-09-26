using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class ChattyParser
    {
        private readonly DownloadService _downloadService;
        private readonly ThreadParser _threadParser;
        private readonly Regex _progressMeterRegex = new Regex(
            "<div class=\"progress\" style=\"width: [0-9]+(\\.[0-9]+)?%\">", RegexOptions.Compiled);
        private readonly Regex _commentCountRegex = new Regex(
            "<a href=\"/chatty\">[0-9,]+ Comments</a>", RegexOptions.Compiled);

        public ChattyParser(DownloadService downloadService, ThreadParser threadParser)
        {
            _downloadService = downloadService;
            _threadParser = threadParser;
        }

        public async Task<(string Html, ChattyPage Page)> GetChattyPage(
            int page, string previousHtml = null, ChattyPage previousChattyPage = null)
        {
            var url = $"https://www.shacknews.com/chatty?page={page}";
            var html = await _downloadService.DownloadWithSharedLogin(url);

            // remove the progress meter which changes on every load, so that we'll get identical html when nothing
            // has changed
            html = _progressMeterRegex.Replace(html, "");

            // remove the comment count too, it appears on all pages even though some pages haven't changed
            html = _commentCountRegex.Replace(html, "");

            return (html, html == previousHtml ? previousChattyPage : ParseChattyPage(html));
        }

        private static readonly string[] _pageNavigationStart = new[] { "<div class=\"pagenavigation\">", ">" };
        private static readonly string[] _pageCurrentPageStart = new[] { "<a rel=\"nofollow\" class=\"selected_page\"", ">" };
        private static readonly string[] _pageChattySettingsStart = new[] { "<div id=\"chatty_settings\" class=\"\">", ">" };
        private static readonly string[] _pageNumThreadsStart = new[] { "<a href=\"/chatty\">", ">" };

        private ChattyPage ParseChattyPage(string html)
        {
            _threadParser.CheckContentId(html);

            var chattyPage = new ChattyPage { Threads = new List<ChattyThread>() };

            var p = new Parser(html);
            p.Seek(1, "<div id=\"chatty_comments_wrap");

            if (p.Peek(1, "<div class=\"pagenavigation\">") == -1)
            {
                chattyPage.CurrentPage = 1;
            }
            else
            {
                p.Seek(1, _pageNavigationStart);

                if (p.Peek(1, "<a rel=\"nofollow\" class=\"selected_page\"") == -1)
                {
                    chattyPage.CurrentPage = 1;
                }
                else
                {
                    chattyPage.CurrentPage = int.Parse(p.Clip(
                        _pageCurrentPageStart,
                        "</a>"));
                }
            }

            p.Seek(1, _pageChattySettingsStart);

            var numThreads = int.Parse(p.Clip(
                _pageNumThreadsStart,
                " Threads"));
            chattyPage.LastPage = (int)Math.Max(Math.Ceiling(numThreads / 40d), 1);

            while (p.Peek(1, "<div class=\"fullpost") != -1)
            {
                try
                {
                    var thread = _threadParser.ParseThreadTree(p);
                    chattyPage.Threads.Add(thread);
                }
                catch (MissingThreadException)
                {
                    // this can be ok, it's possible for a page to only exist because it contains entirely nuked
                    // threads that we can't see
                    break;
                }

                if (chattyPage.Threads.Count > 40)
                    throw new ParsingException("Too many threads. Something is wrong.");
            }

            return chattyPage;
        }

        public async Task<bool> IsModerator(string username, string password)
        {
            var html = await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/moderators", username, password);
            return html.Contains("<div id=\"mod_board_head\">", StringComparison.Ordinal);
        }
    }
}
