using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class SearchResultPage
    {
        public int CurrentPage { get; set; }
        public int TotalResults { get; set; }
        public List<SearchResult> Results { get; set; }
    }
}
