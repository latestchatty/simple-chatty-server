using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class ChattyPage
    {
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public List<ChattyThread> Threads { get; set; }
    }
}
