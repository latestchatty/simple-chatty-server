using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class ChattyAccess
    {
        private readonly ThreadParser _threadParser;
        private readonly LolParser _lolParser;

        public Chatty Chatty { get; private set; }
        public ChattyLolCounts LolCounts { get; private set; }

        public ChattyAccess(ThreadParser threadParser, LolParser lolParser)
        {
            _threadParser = threadParser;
            _lolParser = lolParser;
        }

        public void Update(Chatty chatty, ChattyLolCounts lolCounts)
        {
            Chatty = chatty;
            LolCounts = lolCounts;
        }

        public Chatty GetChatty()
        {
            var chatty = Chatty;
            if (chatty == null)
                throw new Api500Exception(Api500Exception.Codes.SERVER,
                    "The server has just started up and has not yet downloaded the chatty.");
            
            return chatty;
        }

        public ChattyLolCounts GetChattyLolCounts() =>
            LolCounts ?? ChattyLolCounts.Empty;

        public async Task<(ChattyThread, ThreadLolCounts)> GetThread(int postId)
        {
            var chatty = Chatty;

            ChattyThread thread;
            ThreadLolCounts threadLolCounts;
            if (chatty != null && chatty.ThreadsByReplyId.TryGetValue(postId, out thread))
            {
                threadLolCounts =
                    LolCounts?.GetThreadLolCounts(thread.Posts[0].Id)
                    ?? ThreadLolCounts.Empty;
            }
            else
            {
                thread = await _threadParser.GetThread(postId);
                threadLolCounts = await _lolParser.DownloadThreadLolCounts(thread);
            }

            return (thread, threadLolCounts);
        }
    }
}
