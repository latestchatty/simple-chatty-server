using System.Collections.Generic;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetReadStatusResponse
    {
        public List<Thread> Threads { get; set; }

        public sealed class Thread
        {
            public int ThreadId { get; set; }
            public int LastReadPostId { get; set; }
        }
    }
}
