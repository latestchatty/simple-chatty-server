using System.Collections.Generic;
using System.Text.Json.Serialization;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class GetMarkedPostsResponse
    {
        public List<MarkedPost> MarkedPosts { get; set; }

        public sealed class MarkedPost
        {
            public int Id { get; set; }
            [JsonConverter(typeof(MarkedPostTypeConverter))] public MarkedPostType Type { get; set; }
        }
    }
}
