using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;
using SimpleChattyServer.Data.Requests;
using SimpleChattyServer.Data.Responses;
using SimpleChattyServer.Services;
using System.Diagnostics;

namespace SimpleChattyServer.Controllers
{
    [ApiController, Route("v2")]
    public sealed class V2Controller : ControllerBase
    {
        private readonly ChattyProvider _chattyProvider;
        private readonly SearchParser _searchParser;
        private readonly EventProvider _eventProvider;
        private readonly ChattyParser _chattyParser;

        public V2Controller(ChattyProvider chattyProvider, SearchParser searchParser, EventProvider eventProvider,
            ChattyParser chattyParser)
        {
            _chattyProvider = chattyProvider;
            _searchParser = searchParser;
            _eventProvider = eventProvider;
            _chattyParser = chattyParser;
        }

        [HttpGet("getChatty")]
        public GetChattyResponse GetChatty()
        {
            var chatty = _chattyProvider.GetChatty();
            var lolCounts = _chattyProvider.GetChattyLolCounts();
            return new GetChattyResponse
            {
                Threads = (
                    from thread in chatty.Threads
                    let threadLolCounts = lolCounts.GetThreadLolCounts(thread.ThreadId)
                    select new GetChattyResponse.Thread
                    {
                        ThreadId = thread.ThreadId,
                        Posts = PostModel.CreateList(thread, threadLolCounts)
                    }).ToList()
            };
        }

        [HttpGet("getChattyRootPosts")]
        public GetChattyRootPostsResponse GetChattyRootPosts(int offset = 0, int limit = 40, string username = null)
        {
            var chatty = _chattyProvider.GetChatty();
            return new GetChattyRootPostsResponse
            {
                TotalThreadCount = chatty.Threads.Count,
                RootPosts = (
                    from thread in chatty.Threads.Skip(offset).Take(limit)
                    let op = thread.Posts[0]
                    select new GetChattyRootPostsResponse.RootPost
                    {
                        Id = op.Id,
                        Date = op.Date,
                        Author = op.Author,
                        Category = op.Category,
                        Body = op.Body,
                        PostCount = thread.Posts.Count,
                        IsParticipant =
                            username == null
                            ? false
                            : thread.Posts.Any(x => string.Compare(x.Author, username, ignoreCase: true) == 0)
                    }).ToList()
            };
        }

        [HttpGet("getThread")]
        public async Task<GetChattyResponse> GetThread(string id)
        {
            var idList = ParseIntList(id, nameof(id), min: 1, max: 50);
            var list = new List<GetChattyResponse.Thread>(idList.Count);
            foreach (var postId in idList)
            {
                try
                {
                    var (thread, lols) = await _chattyProvider.GetThreadAndLols(postId);
                    list.Add(
                        new GetChattyResponse.Thread
                        {
                            ThreadId = thread.ThreadId,
                            Posts = PostModel.CreateList(thread, lols)
                        });
                }
                catch (MissingThreadException)
                {
                    // silently omit
                }
            }
            return new GetChattyResponse { Threads = list };
        }

        [HttpGet("getThreadPostCount")]
        public async Task<GetThreadPostCountResponse> GetThreadPostCount(string id)
        {
            var idList = ParseIntList(id, nameof(id), min: 1, max: 200);
            var list = new List<GetThreadPostCountResponse.Thread>(idList.Count);
            foreach (var postId in idList)
            {
                var thread = await _chattyProvider.GetThread(postId);
                list.Add(
                    new GetThreadPostCountResponse.Thread
                    {
                        ThreadId = thread.ThreadId,
                        PostCount = thread.Posts.Count
                    });
            }
            return new GetThreadPostCountResponse { Threads = list };
        }

        [HttpGet("getNewestPostInfo")]
        public GetNewestPostInfoResponse GetNewestPostInfo()
        {
            var chatty = _chattyProvider.Chatty;
            if (chatty.Threads.Count == 0)
                return new GetNewestPostInfoResponse { Id = 0, Date = DateTimeOffset.Now };
            var newestPost = default(ChattyPost);
            foreach (var post in chatty.Threads[0].Posts)
                if (newestPost == null || post.Id > newestPost.Id)
                    newestPost = post;
            return new GetNewestPostInfoResponse { Id = newestPost.Id, Date = newestPost.Date };
        }

        [HttpGet("getPost")]
        public async Task<GetPostResponse> GetPost(string id)
        {
            var idSet = ParseIntList(id, nameof(id), min: 1, max: 50).ToHashSet();
            var list = new List<PostModel>(idSet.Count);
            while (idSet.Any())
            {
                var postId = idSet.First();
                var (thread, lols) = await _chattyProvider.GetThreadAndLols(postId);
                var posts = PostModel.CreateList(thread, lols);
                foreach (var post in posts)
                {
                    if (idSet.Contains(post.Id))
                    {
                        idSet.Remove(post.Id);
                        list.Add(post);
                    }
                }
            }
            return new GetPostResponse { Posts = list };
        }

        [HttpPost("postComment")]
        public async Task<SuccessResponse> PostComment(PostCommentRequest request)
        {
            await _chattyProvider.Post(request.Username, request.Password, request.ParentId, request.Text);
            return new SuccessResponse();
        }

        [HttpGet("search")]
        public async Task<GetPostResponse> Search(
            string terms = "", string author = "", string parentAuthor = "", string category = "", int offset = 0,
            int limit = 35, bool oldestFirst = false)
        {
            terms = terms.Trim();
            author = author.Trim();
            parentAuthor = parentAuthor.Trim();
            category = category.Trim();
            
            if (limit > 500)
                throw new Api400Exception("The \"limit\" argument must be 500 or lower.");
            if (terms == "" && author == "" && parentAuthor == "")
                throw new Api400Exception("A search term, author, or parent author query is required.");
            
            var perPage = 15;
            var startingPage = offset / perPage;
            var startingPageFirstIndex = offset - (startingPage * perPage);
            var endingPage = (offset + limit - 1) / perPage;
            var endingPageLastIndex = (offset + limit - 1) - (endingPage * perPage);

            startingPage++;
            endingPage++;

            var list = new List<PostModel>(limit);
            for (var page = startingPage; page <= endingPage; page++)
            {
                var searchResultPage = await _searchParser.Search(terms, author, parentAuthor, category, page, oldestFirst);
                if (!searchResultPage.Results.Any())
                    break;
                for (var i = 0; i < searchResultPage.Results.Count; i++)
                {
                    if (page == startingPage && i < startingPageFirstIndex)
                    {
                        // nothing
                    }
                    else if (page == endingPage && i > endingPageLastIndex)
                    {
                        break;
                    }
                    else
                    {
                        var result = searchResultPage.Results[i];
                        list.Add(
                            new PostModel
                            {
                                Id = result.Id,
                                ThreadId = 0,
                                ParentId = 0,
                                Author = result.Author,
                                Category = ModerationFlag.OnTopic,
                                Date = result.Date,
                                Body = result.Preview,
                                Lols = new List<LolModel>()
                            });
                    }
                }
            }

            return new GetPostResponse { Posts = list };
        }

        [HttpPost("requestReindex")]
        public SuccessResponse RequestReindex()
        {
            return new SuccessResponse();
        }

        [HttpPost("setPostCategory")]
        public async Task<SuccessResponse> SetPostCategory(SetPostCategoryRequest request)
        {
            await _chattyProvider.SetPostCategory(
                request.Username, request.Password, request.PostId, V2ModerationFlagConverter.Parse(request.Category));
            return new SuccessResponse();
        }

        [HttpGet("waitForEvent")]
        public async Task<WaitForEventResponse> WaitForEvent(int lastEventId)
        {
            var stopwatch = Stopwatch.StartNew();
            var maxTime = TimeSpan.FromMinutes(1);
            var pollInterval = TimeSpan.FromSeconds(2.5);

            while (stopwatch.Elapsed < maxTime)
            {
                var events = _eventProvider.GetEvents(lastEventId);
                if (events.Count > 0)
                {
                    return new WaitForEventResponse
                    {
                        LastEventId = events.Last().EventId,
                        Events = events
                    };
                }

                await Task.Delay(pollInterval);
            }

            return new WaitForEventResponse
            {
                LastEventId = lastEventId,
                Events = new List<EventModel>()
            };
        }

        [HttpGet("pollForEvent")]
        public WaitForEventResponse PollForEvent(int lastEventId)
        {
            var events = _eventProvider.GetEvents(lastEventId);
            return new WaitForEventResponse
            {
                LastEventId = events.Count > 0 ? events.Last().EventId : lastEventId,
                Events = events
            };
        }

        [HttpGet("checkConnection")]
        public SuccessResponse CheckConnection()
        {
            return new SuccessResponse();
        }

        [HttpPost("verifyCredentials")]
        public async Task<VerifyCredentialsResponse> VerifyCredentials(VerifyCredentialsRequest request)
        {
            try
            {
                var isModerator = await _chattyParser.IsModerator(request.Username, request.Password);
                return new VerifyCredentialsResponse
                {
                    IsValid = true,
                    IsModerator = isModerator
                };
            }
            catch
            {
                return new VerifyCredentialsResponse
                {
                    IsValid = false,
                    IsModerator = false
                };
            }
        }

        [HttpGet("getAllTenYearUsers")]
        public GetAllTenYearUsersResponse GetAllTenYearUsers()
        {
            return new GetAllTenYearUsersResponse { Users = new List<string>() };
        }

        private static List<int> ParseIntList(string input, string key, int min = 0, int max = int.MaxValue)
        {
            if (min == 0 && (input == "" || input == null))
                return new List<int>();
            var parts = (input ?? "").Split(',');
            if (parts.Length < min)
                throw new Api400Exception($"Parameter \"{key}\" requires at least {min} integer value(s).");
            if (parts.Length > max)
                throw new Api400Exception($"Parameter \"{key}\" requires at most {max} integer value(s).");
            var list = new List<int>(parts.Length);
            list.AddRange(parts.Select(int.Parse));
            return list;
        }
    }
}
