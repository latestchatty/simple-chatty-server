using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class GetChattyRootPostsResponse
    {
        public int TotalThreadCount { get; set; }
        public List<RootPost> RootPosts { get; set; }
        
        public sealed class RootPost
        {
            public int Id { get; set; }
            [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
            public string Author { get; set; }
            [JsonConverter(typeof(CortexModerationFlagConverter))] public ModerationFlag Category { get; set; }
            public string Body { get; set; }
            public int PostCount { get; set; }
            public bool IsParticipant { get; set; }
        }
    }
}
