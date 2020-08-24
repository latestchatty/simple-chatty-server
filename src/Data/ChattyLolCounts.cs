using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class ChattyLolCounts
    {
        [JsonConverter(typeof(IntDictionaryConverter<ThreadLolCounts>))]
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
