using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1RootCommentModel
    {
        public string Id { get; set; }
        [JsonPropertyName("reply_count")] public int ReplyCount { get; set; }
        public string Body { get; set; }
        [JsonConverter(typeof(V1DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        public List<V1ParticipantModel> Participants { get; set; }
        [JsonConverter(typeof(V1ModerationFlagConverter))] public ModerationFlag Category { get; set; }
        [JsonPropertyName("last_reply_id")] public string LastReplyId { get; set; }
        public string Author { get; set; }
        public string Preview { get; set; }
        public List<V1CommentModel> Comments { get; set; }
    }
}
