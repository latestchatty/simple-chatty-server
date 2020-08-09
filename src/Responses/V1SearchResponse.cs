using System.Collections.Generic;
using System.Text.Json.Serialization;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class V1SearchResponse
    {
        public string Terms { get; set; }
        public string Author { get; set; }
        [JsonPropertyName("parent_author")] public string ParentAuthor { get; set; }
        [JsonPropertyName("last_page")] public int LastPage { get; set; }
        public List<V1SearchResultModel> Comments { get; set; }
    }
}
