using System.Collections.Generic;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetMarkedPostsResponse
    {
        public List<MarkedPostModel> MarkedPosts { get; set; }
    }
}
