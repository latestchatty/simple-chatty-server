using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class ChattyLolCounts
    {
        public Dictionary<int, ThreadLolCounts> CountsByThreadId { get; set; }

        public ThreadLolCounts GetThreadLolCounts(int threadId) =>
            CountsByThreadId.TryGetValue(threadId, out var threadDict)
            ? threadDict
            : ThreadLolCounts.Empty;

        public static ChattyLolCounts Empty =>
            new ChattyLolCounts
            {
                CountsByThreadId = new Dictionary<int, ThreadLolCounts>()
            };
    }
}
