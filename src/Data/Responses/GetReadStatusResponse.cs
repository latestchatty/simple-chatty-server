using System.Collections.Generic;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetReadStatusResponse
    {
        public List<GetReadStatusResponseThread> Threads { get; set; }

        public sealed class GetReadStatusResponseThread
        {
            public int ThreadId { get; set; }
            public int LastReadPostId { get; set; }
        }
    }
}
