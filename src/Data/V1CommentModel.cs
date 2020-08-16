using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1CommentModel
    {
        public string Id { get; set; }
        public string Body { get; set; }
        [JsonConverter(typeof(V1DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        [JsonConverter(typeof(V1ModerationFlagConverter))] public ModerationFlag Category { get; set; }
        public string Author { get; set; }
        public string Preview { get; set; }
        public List<V1CommentModel> Comments { get; set; }
    }
}
