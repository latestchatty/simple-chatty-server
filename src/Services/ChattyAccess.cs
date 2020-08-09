using System.Threading.Tasks;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public sealed class ChattyAccess
    {
        private readonly ScrapeService _scrapeService;
        private readonly ThreadParser _threadParser;

        public ChattyAccess(ScrapeService scrapeService, ThreadParser threadParser)
        {
            _scrapeService = scrapeService;
            _threadParser = threadParser;
        }

        public async Task<ChattyThread> GetThread(int id)
        {
            var chatty = _scrapeService.Chatty;
            if (chatty != null && chatty.ThreadsByReplyId.TryGetValue(id, out var thread))
                return thread;
            
            return await _threadParser.GetThread(id);
        }
    }
}
