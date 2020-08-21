using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Data.Responses
{
    public sealed class WaitForEventResponse
    {
        public int LastEventId { get; set; }
        public List<EventModel> Events { get; set; }
        public bool TooManyEvents { get; set; }
    }
}
