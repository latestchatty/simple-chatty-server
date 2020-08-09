using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class GetMessagesResponse
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalMessages { get; set; }
        public List<MessageModel> Messages { get; set; }
    }
}
