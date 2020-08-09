using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class ThreadLolCounts
    {
        public Dictionary<int, List<LolModel>> CountsByPostId { get; set; }

        public static ThreadLolCounts Empty =>
            new ThreadLolCounts
            {
                CountsByPostId = new Dictionary<int, List<LolModel>>()
            };
    }
}
