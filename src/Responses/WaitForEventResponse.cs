using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class WaitForEventResponse
    {
        public int LastEventId { get; set; }
        public List<EventModel> Events { get; set; }
    }
}
