using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class Chatty
    {
        public List<ChattyThread> Threads { get; set; }
        public Dictionary<int, ChattyThread> ThreadsByRootId { get; set; }
        public Dictionary<int, ChattyThread> ThreadsByReplyId { get; set; }
        public Dictionary<int, ChattyPost> PostsById { get; set; }
    }
}
