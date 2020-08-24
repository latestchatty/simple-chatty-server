using System.Collections.Generic;

namespace SimpleChattyServer.Data
{
    public sealed class ScrapeState
    {
        public Chatty Chatty { get; set; }
        public List<Page> Pages { get; set; }
        public string LolJson { get; set; }
        public ChattyLolCounts LolCounts { get; set; }
        public List<EventModel> Events { get; set; }

        public sealed class Page
        {
            public string Html { get; set; }
            public ChattyPage ChattyPage { get; set; }
        }
    }
}
