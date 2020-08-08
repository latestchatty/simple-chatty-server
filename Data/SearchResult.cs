using System;

namespace SimpleChattyServer.Data
{
    public sealed class SearchResult
    {
        public int Id { get; set; }
        public string Preview { get; set; }
        public string Author { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
