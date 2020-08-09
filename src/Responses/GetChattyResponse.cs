using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class GetChattyResponse
    {
        public List<Thread> Threads { get; set; }

        public sealed class Thread
        {
            public int ThreadId { get; set; }
            public List<PostModel> Posts { get; set; }
        }
    }
}
