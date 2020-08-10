using System;
using System.Collections.Generic;
using SimpleChattyServer.Data;

namespace SimpleChattyServer.Services
{
    public sealed class EventProvider
    {
        private const int MAX_EVENTS = 10_000;
        private Chatty _chatty;

        private readonly int _initialEventId;
        private readonly List<EventModel> _events = new List<EventModel>(MAX_EVENTS);

        public EventProvider()
        {
            // arbitrarily pick the first event id so that after restarting the server, the ids keep going up vs. the
            // ids before restarting
            _initialEventId = (int)Math.Max(0,
                (DateTimeOffset.Now - new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
        }

        public void Update(Chatty chatty)
        {
            throw new NotImplementedException();
        }
    }
}
