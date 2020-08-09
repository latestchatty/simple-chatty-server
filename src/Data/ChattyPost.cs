using System;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class ChattyPost
    {
        public int Id { get; set; }
        public int Depth { get; set; }
        public string Category { get; set; }
        public string Author { get; set; }
        public string Body { get; set; }
        public string Preview { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
