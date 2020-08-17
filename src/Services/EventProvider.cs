using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class EventProvider
    {
        private const int MAX_EVENTS = 10_000;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly List<EventModel> _events = new List<EventModel>(MAX_EVENTS);
        private Chatty _chatty;
        private ChattyLolCounts _chattyLolCounts;

        private readonly ThreadParser _threadParser;

        public EventProvider(ThreadParser threadParser)
        {
            _threadParser = threadParser;
        }

        public async Task Update(Chatty newChatty, ChattyLolCounts newChattyLolCounts)
        {
            await Task.Run(() =>
            {
                _lock.EnterWriteLock();
                try
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
                        if (DoesThreadExist(oldThreadId).GetAwaiter().GetResult())
                        {
                            // expired thread -- no event needed
                        }
                        else
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

                            if (oldPost.Author != newPost.Author || oldPost.Body != newPost.Body)
                            {
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
                        }
                    }

                    _chatty = newChatty;
                    _chattyLolCounts = newChattyLolCounts;
                    _events.AddRange(newEvents);

                    while (_events.Count > MAX_EVENTS)
                        _events.RemoveAt(0);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            });
        }

        public async Task SendReadStatusUpdateEvent(string username)
        {
            await Task.Run(() =>
            {
                _lock.EnterWriteLock();
                try
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
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            });

        }

        public List<EventModel> GetEvents(int lastEventId)
        {
            _lock.EnterReadLock();
            try
            {
                var minEventId = _events.Count == 0 ? 0 : _events[0].EventId;
                var maxEventId = _events.Count == 0 ? 0 : _events[_events.Count - 1].EventId;
                if (lastEventId < minEventId || lastEventId > maxEventId)
                    throw new Api400Exception(Api400Exception.Codes.TOO_MANY_EVENTS);

                var list = new List<EventModel>();
                for (var i = _events.Count - 1; i >= 0 && _events[i].EventId > lastEventId; i--)
                    list.Add(_events[i]);

                list.Reverse();
                return list;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int GetLastEventId()
        {
            _lock.EnterReadLock();
            try
            {
                return _events.Count == 0 ? 0 : _events.Last().EventId;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private static Dictionary<int, PostModel> GetPostModelsById(
            ChattyThread thread, ChattyLolCounts lolCounts)
        {
            var threadLolCounts = lolCounts.GetThreadLolCounts(thread.ThreadId);
            return PostModel.CreateList(thread, threadLolCounts).ToDictionary(x => x.Id);
        }

        private async Task<bool> DoesThreadExist(int threadId)
        {
            try
            {
                var missingThread = await _threadParser.GetThread(threadId);
                return true;
            }
            catch (MissingThreadException)
            {
                return false;
            }
        }
    }
}
