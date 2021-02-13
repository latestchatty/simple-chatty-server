using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class EventProvider
    {
        private const int MAX_EVENTS = 10_000;

        private readonly ILogger<EventProvider> _logger;
        private readonly ThreadParser _threadParser;

        private readonly LoggedReaderWriterLock _lock;
        private readonly List<EventModel> _events = new List<EventModel>(MAX_EVENTS);
        private Chatty _chatty;
        private ChattyLolCounts _chattyLolCounts;

        public EventProvider(ILogger<EventProvider> logger, ThreadParser threadParser)
        {
            _logger = logger;
            _threadParser = threadParser;

            _lock = new LoggedReaderWriterLock(
                nameof(EventProvider),
                x => _logger.LogDebug(x));
        }

        public async Task<List<EventModel>> CloneEventsList()
        {
            return await _lock.WithReadLock(nameof(CloneEventsList),
                func: () => new List<EventModel>(_events));
        }

        public async Task Start(Chatty newChatty, ChattyLolCounts newChattyLolCounts)
        {
            if (_chatty != null || _chattyLolCounts != null)
                throw new InvalidOperationException($"{nameof(EventProvider)} already started.");

            await _lock.WithWriteLock(nameof(Start),
                action: () =>
                {
                    _chatty = newChatty;
                    _chattyLolCounts = newChattyLolCounts;
                });
        }

        public async Task PrePopulate(Chatty newChatty, ChattyLolCounts newChattyLolCounts, List<EventModel> events)
        {
            await _lock.WithWriteLock(nameof(PrePopulate),
                action: () =>
                {
                    _chatty = newChatty;
                    _chattyLolCounts = newChattyLolCounts;
                    _events.Clear();
                    _events.AddRange(events);
                });
        }

        public async Task Update(Chatty newChatty, ChattyLolCounts newChattyLolCounts)
        {
            var oldChatty = await _lock.WithReadLock(nameof(Update), func: () => _chatty);

            if (oldChatty != null)
            {
                // safety first, this should never throw if the scraper has done its job correctly
                foreach (var oldThread in oldChatty.Threads)
                {
                    if (!newChatty.ThreadsByRootId.ContainsKey(oldThread.ThreadId) &&
                        !newChatty.NukedThreadIds.Contains(oldThread.ThreadId) &&
                        !newChatty.ExpiredThreadIds.Contains(oldThread.ThreadId))
                    {
                        throw new Exception($"Thread ID {oldThread.ThreadId} disappeared but is not listed as expired or nuked!");
                    }
                }
            }

            await _lock.WithWriteLock(nameof(Update),
                action: () =>
                {
                    if (_chatty == null || _chattyLolCounts == null)
                    {
                        _chatty = newChatty;
                        _chattyLolCounts = newChattyLolCounts;
                        return;
                    }

                    var nextEventId = _events.Count == 0 ? 1 : _events.Last().EventId + 1;
                    var newEvents = new List<EventModel>();

                    foreach (var oldThreadId in
                        from thread in _chatty.Threads
                        where !newChatty.ThreadsByRootId.ContainsKey(thread.ThreadId)
                        select thread.ThreadId)
                    {
                        // was this thread nuked or did it expire?
                        if (newChatty.NukedThreadIds.Contains(oldThreadId))
                        {
                            // nuked thread
                            newEvents.Add(
                                new EventModel
                                {
                                    EventId = nextEventId++,
                                    EventDate = DateTimeOffset.Now,
                                    EventType = EventType.CategoryChange,
                                    EventData = new CategoryChangeEventDataModel
                                    {
                                        PostId = oldThreadId,
                                        Category = ModerationFlag.Nuked
                                    }
                                });
                        }
                    }

                    foreach (var newThread in
                        from thread in newChatty.Threads
                        where !_chatty.ThreadsByRootId.ContainsKey(thread.ThreadId)
                        select thread)
                    {
                        // new thread
                        var newPostModels = GetPostModelsById(newThread, newChattyLolCounts);
                        foreach (var newPost in newPostModels.Values.OrderBy(x => x.Id))
                        {
                            newEvents.Add(
                                new EventModel
                                {
                                    EventId = nextEventId++,
                                    EventDate = DateTimeOffset.Now,
                                    EventType = EventType.NewPost,
                                    EventData = new NewPostEventDataModel
                                    {
                                        PostId = newPost.Id,
                                        Post = newPost,
                                        ParentAuthor = newPost.ParentId == 0 ? "" : newPostModels[newPost.ParentId].Author
                                    }
                                });
                        }
                    }

                    foreach (var newThread in newChatty.Threads)
                    {
                        if (!_chatty.ThreadsByRootId.TryGetValue(newThread.ThreadId, out var oldThread))
                            continue;

                        var oldPostModels = GetPostModelsById(oldThread, _chattyLolCounts);
                        var newPostModels = GetPostModelsById(newThread, newChattyLolCounts);

                        foreach (var oldPost in
                            from postPair in oldPostModels
                            where !newPostModels.ContainsKey(postPair.Key)
                            orderby postPair.Key
                            select postPair.Value)
                        {
                            // nuked post
                            newEvents.Add(
                                new EventModel
                                {
                                    EventId = nextEventId++,
                                    EventDate = DateTimeOffset.Now,
                                    EventType = EventType.CategoryChange,
                                    EventData = new CategoryChangeEventDataModel
                                    {
                                        PostId = oldPost.Id,
                                        Category = ModerationFlag.Nuked
                                    }
                                });
                        }

                        foreach (var newPost in
                            from postPair in newPostModels
                            where !oldPostModels.ContainsKey(postPair.Key)
                            orderby postPair.Key
                            select postPair.Value)
                        {
                            // new post
                            newEvents.Add(
                                new EventModel
                                {
                                    EventId = nextEventId++,
                                    EventDate = DateTimeOffset.Now,
                                    EventType = EventType.NewPost,
                                    EventData = new NewPostEventDataModel
                                    {
                                        PostId = newPost.Id,
                                        Post = newPost,
                                        ParentAuthor = newPost.ParentId == 0 ? "" : newPostModels[newPost.ParentId].Author
                                    }
                                });
                        }

                        foreach (var newPost in newPostModels.Values)
                        {
                            if (!oldPostModels.TryGetValue(newPost.Id, out var oldPost))
                                continue;

                            if (oldPost.Category != newPost.Category)
                            {
                                newEvents.Add(
                                    new EventModel
                                    {
                                        EventId = nextEventId++,
                                        EventDate = DateTimeOffset.Now,
                                        EventType = EventType.CategoryChange,
                                        EventData = new CategoryChangeEventDataModel
                                        {
                                            PostId = newPost.Id,
                                            Category = newPost.Category
                                        }
                                    });
                            }

                            if (!Enumerable.SequenceEqual(
                                oldPost.Lols.OrderBy(x => x.Tag),
                                newPost.Lols.OrderBy(x => x.Tag)))
                            {
                                newEvents.Add(
                                    new EventModel
                                    {
                                        EventId = nextEventId++,
                                        EventDate = DateTimeOffset.Now,
                                        EventType = EventType.LolCountsUpdate,
                                        EventData = new LolCountsUpdateEventDataModel
                                        {
                                            Updates = (
                                                from x in newPost.Lols
                                                select new LolCountUpdateModel
                                                {
                                                    PostId = newPost.Id,
                                                    Tag = x.Tag,
                                                    Count = x.Count
                                                }).ToList()
                                        }
                                    });
                            }

                            if (oldPost.Author != newPost.Author) // detect deleted users
                            {
                                _logger.LogInformation($"postChange: {oldPost.Author} -> {newPost.Author}.");
                                newEvents.Add(
                                    new EventModel
                                    {
                                        EventId = nextEventId++,
                                        EventDate = DateTimeOffset.Now,
                                        EventType = EventType.PostChange,
                                        EventData = new PostChangeEventDataModel
                                        {
                                            PostId = newPost.Id
                                        }
                                    });
                            }

                            if (oldPost.IsFrozen != newPost.IsFrozen)
                            {
                                _logger.LogInformation($"Post {newPost.Id} freeze change: {oldPost.IsFrozen} -> {newPost.IsFrozen}.");
                                newEvents.Add(
                                    new EventModel
                                    {
                                        EventId = nextEventId++,
                                        EventDate = DateTimeOffset.Now,
                                        EventType = EventType.PostFreezeChange,
                                        EventData = new PostFrozenChangeEventDataModel
                                        {
                                            PostId = newPost.Id,
                                            Frozen = newPost.IsFrozen
                                        }
                                    }
                                );
                            }
                        }
                    }

                    _chatty = newChatty;
                    _chattyLolCounts = newChattyLolCounts;
                    _events.AddRange(newEvents);

                    while (_events.Count > MAX_EVENTS)
                        _events.RemoveAt(0);
                });
        }

        public async Task SendReadStatusUpdateEvent(string username)
        {
            await _lock.WithWriteLock(nameof(SendReadStatusUpdateEvent),
                action: () =>
                {
                    _events.Add(
                        new EventModel
                        {
                            EventId = _events.Count == 0 ? 1 : _events.Last().EventId + 1,
                            EventDate = DateTimeOffset.Now,
                            EventType = EventType.ReadStatusUpdate,
                            EventData = new ReadStatusUpdateEventDataModel
                            {
                                Username = username
                            }
                        });
                });
        }

        public async Task<List<EventModel>> GetEvents(int lastEventId)
        {
            return await _lock.WithReadLock(nameof(GetEvents),
                func: () =>
                {
                    var minEventId = _events.Count == 0 ? 0 : _events[0].EventId;
                    var maxEventId = _events.Count == 0 ? 0 : _events[_events.Count - 1].EventId;
                    if (lastEventId < minEventId - 1 || lastEventId > maxEventId)
                        return null;

                    var list = new List<EventModel>();
                    for (var i = _events.Count - 1; i >= 0 && _events[i].EventId > lastEventId; i--)
                        list.Add(_events[i]);

                    list.Reverse();
                    return list;
                });
        }

        public async Task<int> GetLastEventId()
        {
            return await _lock.WithReadLock(nameof(GetLastEventId),
                func: () => _events.Count == 0 ? 0 : _events.Last().EventId);
        }

        private static Dictionary<int, PostModel> GetPostModelsById(
            ChattyThread thread, ChattyLolCounts lolCounts)
        {
            var threadLolCounts = lolCounts.GetThreadLolCounts(thread.ThreadId);
            return PostModel.CreateList(thread, threadLolCounts).ToDictionary(x => x.Id);
        }
    }
}
