using System;
using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class PostModel
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public int ParentId { get; set; }
        public string Author { get; set; }
        public ModerationFlag Category { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Body { get; set; }
        public List<LolModel> Lols { get; set; }
    }
}
