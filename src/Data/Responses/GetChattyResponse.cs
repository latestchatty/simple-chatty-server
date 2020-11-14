using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetChattyResponse
    {
        public List<GetChattyResponseThread> Threads { get; set; }

        public sealed class GetChattyResponseThread
        {
            public int ThreadId { get; set; }
            public List<PostModel> Posts { get; set; }
        }
    }
}
