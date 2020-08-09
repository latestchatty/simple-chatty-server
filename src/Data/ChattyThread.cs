using System;
using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class ChattyThread
    {
        public List<ChattyPost> Posts { get; set; }

        public int ThreadId => Posts[0].Id;
    }
}
