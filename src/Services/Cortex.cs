using System.Text.RegularExpressions;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public static class Cortex
    {
        private static readonly Regex m_cortex_regex = new Regex(
            @"Read more: <a href=""[^""]*/cortex/article/[^""]+""[^>]*>[^h][^t][^t][^p]",
            RegexOptions.Compiled);
            
        public static Chatty DetectCortexThreads(Chatty chatty)
        {
            foreach (var thread in chatty.Threads)
                DetectCortexThread(thread);
            return chatty;
        }

        public static ChattyThread DetectCortexThread(ChattyThread thread)
        {
            var op = thread.Posts[0];
            op.IsCortex = m_cortex_regex.IsMatch(op.Body);
            return thread;
        }
    }
}
