using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class GetPostResponse
    {
        public List<PostModel> Posts { get; set; }
    }
}
