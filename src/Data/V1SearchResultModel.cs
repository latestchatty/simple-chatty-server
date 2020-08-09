using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1SearchResultModel
    {
        public List<string> Comments { get; set; } = new List<string>();
        [JsonPropertyName("last_reply_id")] public string LastReplyId { get; set; } = null;
        public string Author { get; set; }
        [JsonConverter(typeof(V1DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        [JsonPropertyName("story_id")] public int StoryId { get; set; } = 0;
        public string Category { get; set; } = null;
        [JsonPropertyName("reply_count")] public string ReplyCount { get; set; } = null;
        public string Id { get; set; }
        [JsonPropertyName("story_name")] public string StoryName { get; set; } = "Latest Chatty";
        public string Preview { get; set; }
        public string Body { get; set; } = null;
    }
}
