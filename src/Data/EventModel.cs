using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    [JsonConverter(typeof(EventModelConverter))]
    public sealed class EventModel
    {
        public int EventId { get; set; }
        public DateTimeOffset EventDate { get; set; }
        public EventType EventType { get; set; }
        public EventDataModel EventData { get; set; }
    }    
}
