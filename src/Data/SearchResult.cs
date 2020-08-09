using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class SearchResult
    {
        public int Id { get; set; }
        public string Preview { get; set; }
        public string Author { get; set; }
        [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
    }
}
