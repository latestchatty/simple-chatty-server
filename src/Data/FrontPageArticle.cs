using System;

namespace SimpleChattyServer.Data
{
    public sealed class FrontPageArticle
    {
        public string Body { get; set; }
        public DateTimeOffset Date { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Preview { get; set; }
        public string Url { get; set; }
    }
}
