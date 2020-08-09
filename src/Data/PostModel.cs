using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class PostModel
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public int ParentId { get; set; }
        public string Author { get; set; }
        [JsonConverter(typeof(V2ModerationFlagConverter))] public ModerationFlag Category { get; set; }
        [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        public string Body { get; set; }
        public List<LolModel> Lols { get; set; }
    }
}
