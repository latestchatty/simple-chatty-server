using System;

namespace SimpleChattyServer.Data
{
    public sealed class EventModel
    {
        public int EventId { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public EventType EventType { get; set; }
        public EventDataModel EventData { get; set; }
    }    
}
