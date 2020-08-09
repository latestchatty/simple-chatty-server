using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class ThreadLolCounts
    {
        public Dictionary<int, Dictionary<string, int>> CountsByPostIdThenTag { get; set; }

        public static ThreadLolCounts Empty =>
            new ThreadLolCounts
            {
                CountsByPostIdThenTag = new Dictionary<int, Dictionary<string, int>>()
            };
    }
}
