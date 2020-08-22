using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public sealed class SearchParser
    {
        private readonly DownloadService _downloadService;

        public SearchParser(DownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task<SearchResultPage> Search(
            string terms, string author, string parentAuthor, string category, int page, bool oldestFirst = false)
        {
            if (string.IsNullOrEmpty(category))
                category = "all";

            var query = _downloadService.NewQuery();
            query.Add("chatty", "1");
            query.Add("type", "4");
            query.Add("chatty_term", terms);
            query.Add("chatty_user", author);
            query.Add("chatty_author", parentAuthor);
            query.Add("chatty_filter", category);
            query.Add("page", $"{page}");
            query.Add("result_sort", oldestFirst ? "postdate_asc" : "postdate_desc");

            var html = await _downloadService.DownloadWithSharedLogin(
                "http://www.shacknews.com/search?" + query.ToString());

            return await Task.Run(() =>
            {
                var p = new Parser(html);
                var searchResultPage =
                    new SearchResultPage
                    {
                        Results = new List<SearchResult>(),
                        CurrentPage = page
                    };

                searchResultPage.TotalResults = int.Parse(p.Clip(
                    new[] { "<h2 class=\"search-num-found\"", ">" },
                    " ").Replace(",", ""));

                while (p.Peek(1, "<li class=\"result") != -1 &&
                    p.Peek(1, "<span class=\"chatty-author\">") != -1)
                {
                    var result = new SearchResult();
                    result.Author = p.Clip(
                        new[] { "<span class=\"chatty-author\">", "<a class=\"more\"", ">" },
                        ":</a></span>");
                    result.Date = DateParser.Parse(p.Clip(
                        new[] { "<span class=\"postdate\"", ">", " " },
                        "</span>"));
                    result.Id = int.Parse(p.Clip(
                        new[] { "<a href=\"/chatty", "chatty/", "/" },
                        "\""));
                    result.Preview = p.Clip(
                        new[] { ">" },
                        "</a>");
                    searchResultPage.Results.Add(result);
                }

                return searchResultPage;
            });
        }
    }
}
