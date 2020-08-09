using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1StoryModel
    {
        public string Preview { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        [JsonConverter(typeof(V1DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        [JsonPropertyName("comment_count")] public int CommentCount { get; set; }
        public int Id { get; set; }
        [JsonPropertyName("thread_id")] public int ThreadId { get; set; }
    }
}
