using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class GetMarkedPostsResponse
    {
        public List<MarkedPost> MarkedPosts { get; set; }

        public sealed class MarkedPost
        {
            public int Id { get; set; }
            public MarkedPostType Type { get; set; }
        }
    }
}
