using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class MessageModel
    {
        public int Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        public string Body { get; set; }
        public bool Unread { get; set; }
    }
}
