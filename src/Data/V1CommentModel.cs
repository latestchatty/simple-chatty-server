using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1CommentModel
    {
        public List<V1CommentModel> Comments { get; set; }
        [JsonPropertyName("reply_count")] public int ReplyCount { get; set; }
        public string Body { get; set; }
        [JsonConverter(typeof(V1DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        public List<string> Participants { get; set; }
        public ModerationFlag Category { get; set; }
        [JsonPropertyName("last_reply_id")] public string LastReplyId { get; set; }
        public string Author { get; set; }
        public string Preview { get; set; }
        public string Id { get; set; }
    }
}
