using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class ChattyParser
    {
        private readonly DownloadService _downloadService;
        private readonly ThreadParser _threadParser;

        public ChattyParser(DownloadService downloadService, ThreadParser threadParser)
        {
            _downloadService = downloadService;
            _threadParser = threadParser;
        }

        public async Task<ChattyPage> GetChattyPage(int page)
        {
            var url = $"https://www.shacknews.com/chatty?page={page}";
            var html = await _downloadService.DownloadWithSharedLogin(url);
            return ParseChattyPage(html);
        }

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
                p.Seek(1, new[] { "<div class=\"pagenavigation\">", ">" });

                if (p.Peek(1, "<a rel=\"nofollow\" class=\"selected_page\"") == -1)
                {
                    chattyPage.CurrentPage = 1;
                }
                else
                {
                    chattyPage.CurrentPage = int.Parse(p.Clip(
                        new[] { "<a rel=\"nofollow\" class=\"selected_page\"", ">" },
                        "</a>"));
                }
            }

            p.Seek(1, new[] { "<div id=\"chatty_settings\" class=\"\">", ">" });

            var numThreads = int.Parse(p.Clip(
                new[] { "<a href=\"/chatty\">", ">" },
                " Threads"));
            chattyPage.LastPage = (int)Math.Max(Math.Ceiling(numThreads / 40d), 1);

            while (p.Peek(1, "<div class=\"fullpost") != -1)
            {
                var thread = _threadParser.ParseThreadTree(p);
                chattyPage.Threads.Add(thread);

                if (chattyPage.Threads.Count > 40)
                    throw new ParsingException("Too many threads. Something is wrong.");
            }

            return chattyPage;
        }
    }
}
