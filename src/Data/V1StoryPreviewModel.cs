using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1StoryPreviewModel
    {
        public string Body { get; set; }
        [JsonPropertyName("comment_count")] public int CommentCount { get; set; }
        [JsonConverter(typeof(V1DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Preview { get; set; }
        public string Url { get; set; }
        [JsonPropertyName("thread_id")] public string ThreadId { get; set; } = "";
    }
}
