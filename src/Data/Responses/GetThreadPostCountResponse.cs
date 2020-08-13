using System.Collections.Generic;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetThreadPostCountResponse
    {
        public List<Thread> Threads { get; set; }

        public sealed class Thread
        {
            public int ThreadId { get; set; }
            public int PostCount { get; set; }
        }
    }
}
