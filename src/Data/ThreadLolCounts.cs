using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class ThreadLolCounts
    {
        [JsonConverter(typeof(IntDictionaryConverter<List<LolModel>>))]
        public Dictionary<int, List<LolModel>> CountsByPostId { get; set; }

        public static ThreadLolCounts Empty =>
            new ThreadLolCounts
            {
                CountsByPostId = new Dictionary<int, List<LolModel>>()
            };
    }
}
