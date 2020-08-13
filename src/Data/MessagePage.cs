using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class MessagePage
    {
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public int TotalCount { get; set; }
        public List<MessageModel> Messages { get; set; }
    }
}
