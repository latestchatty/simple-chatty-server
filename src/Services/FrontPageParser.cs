using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public sealed class FrontPageParser
    {
        private readonly DownloadService _downloadService;

        public FrontPageParser(DownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task<List<FrontPageArticle>> GetStories()
        {
            var html = await _downloadService.DownloadWithSharedLogin("https://www.shacknews.com/feed/rss",
                verifyLoginStatus: false);
            return await Task.Run(() =>
            {
                var list = new List<FrontPageArticle>();
                var p = new Parser(html);
                p.Seek(1, "<channel>");
                while (p.Peek(1, "<item>") != -1)
                {
                    p.Seek(1, "<item>");
                    var endPos = p.Peek(1, "</item>");
                    var title = p.Clip(
                        new[] { "<title><![CDATA[", "CDATA[", "[" },
                        "]]></title>");
                    var link = p.Clip(
                        new[] { "<link>", ">" },
                        "</link>");
                    var pubDateStr = p.Clip(
                        new[] { "<pubDate>", ">" },
                        "</pubDate>");
                    var time = DateParser.Parse(pubDateStr);
                    // the time zone offset in this time is a lie. this is really UTC.
                    time = new DateTimeOffset(time.DateTime, TimeSpan.Zero);
                    var description = p.Clip(
                        new[] { "<description><![CDATA[", "CDATA[", "[" },
                        "]]></description>");
                    var linkParts = link.Split('/');
                    var id = int.Parse(linkParts[linkParts.Length - 2]);
                    list.Add(
                        new FrontPageArticle
                        {
                            Body = description,
                            Date = time,
                            Id = id,
                            Name = title,
                            Preview = description,
                            Url = link
                        });
                }
                return list;
            });
        }

        public async Task<V1StoryModel> GetArticle(int storyId)
        {
            var html = await _downloadService.DownloadWithSharedLogin($"https://www.shacknews.com/article/{storyId}");
            return await Task.Run(() =>
            {
                var p = new Parser(html);
                p.Seek(1, "<div class=\"article-lead-middle\">");
                var name = WebUtility.HtmlDecode(p.Clip(
                    new[] { "<h1 class=\"article-title\">", ">" },
                    "</h1>"));
                var preview = WebUtility.HtmlDecode(p.Clip(
                    new[] { "<description>", "<p>", ">" },
                    "</p>"));
                p.Seek(1, "<div class=\"article-lead-bottom\">");
                var date = DateParser.Parse(p.Clip(
                    new[] { "<time datetime=\"", "\"" },
                    "\">"));
                var body = "<p>" + p.Clip(
                    new[] { "<p>", ">" },
                    "<div class=\"author-short-bio");
                return new V1StoryModel
                {
                    Preview = preview,
                    Name = name,
                    Body = body,
                    Date = date,
                    CommentCount = 0,
                    Id = storyId,
                    ThreadId = 0
                };
            });
        }
    }
}
