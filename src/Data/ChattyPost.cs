using System;

namespace SimpleChattyServer.Data
{
    public sealed class ChattyPost
    {
        public int Id { get; set; }
        public int Depth { get; set; }
        public ModerationFlag Category { get; set; }
        public string Author { get; set; }
        public int AuthorId { get; set; }
        public UserFlair AuthorFlair { get; set;}
        public string Body { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool IsCortex { get; set; }
        public bool IsFrozen { get; set; }
    }
}
