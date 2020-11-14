using System.Collections.Generic;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class GetThreadPostCountResponse
    {
        public List<GetThreadPostCountResponseThread> Threads { get; set; }

        public sealed class GetThreadPostCountResponseThread
        {
            public int ThreadId { get; set; }
            public int PostCount { get; set; }
        }
    }
}
