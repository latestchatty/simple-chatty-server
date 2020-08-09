using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class EventModel
    {
        public int EventId { get; set; }
        [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset EventDate { get; set; }
        public EventType EventType { get; set; }
        public EventDataModel EventData { get; set; }
    }    
}
