using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Responses
{
    public sealed class V1MessagesResponse
    {
        public string User { get; set; }
        public List<Message> Messages { get; set; }

        public sealed class Message
        {
            public string Id { get; set; }
            public string From { get; set; }
            public string To { get; set; }
            public string Subject { get; set; }
            [JsonConverter(typeof(V1MessageDateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
            public string Body { get; set; }
            public bool Unread { get; set; }
        }
    }
}
