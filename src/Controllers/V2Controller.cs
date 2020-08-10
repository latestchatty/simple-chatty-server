using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;
using SimpleChattyServer.Responses;
using SimpleChattyServer.Services;

namespace SimpleChattyServer.Controllers
{
    [ApiController, Route("v2")]
    public sealed class V2Controller : ControllerBase
    {
        private readonly ChattyProvider _chattyProvider;
        private readonly SearchParser _searchParser;

        public V2Controller(ChattyProvider chattyProvider, SearchParser searchParser)
        {
            _chattyProvider = chattyProvider;
            _searchParser = searchParser;
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
                        Posts = CreatePostModelList(thread, threadLolCounts)
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
                            Posts = CreatePostModelList(thread, lols)
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
                var posts = CreatePostModelList(thread, lols);
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

        [HttpGet("requestReindex")]
        public SuccessResponse RequestReindex()
        {
            return new SuccessResponse();
        }

        [HttpPost("setPostCategory")]
        public async Task<SuccessResponse> SetPostCategory(string username, string password, int postId, string category)
        {
            await _chattyProvider.SetPostCategory(username, password, postId, V2ModerationFlagConverter.Parse(category));
            return new SuccessResponse();
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
        
        private static List<PostModel> CreatePostModelList(ChattyThread thread, ThreadLolCounts lolCounts)
        {
            var list = new List<PostModel>(thread.Posts.Count);
            var maxDepth = thread.Posts.Max(x => x.Depth);
            var lastIdAtDepth = new int[maxDepth + 1];

            foreach (var post in thread.Posts)
            {
                lastIdAtDepth[post.Depth] = post.Id;
                list.Add(
                    new PostModel
                    {
                        Id = post.Id,
                        ThreadId = thread.ThreadId,
                        ParentId = post.Depth == 0 ? 0 : lastIdAtDepth[post.Depth - 1],
                        Author = post.Author,
                        Category = post.Category,
                        Date = post.Date,
                        Body = post.Body,
                        Lols =
                            lolCounts.CountsByPostId.TryGetValue(post.Id, out var postLols)
                            ? postLols
                            : new List<LolModel>()
                    });
            }

            return list;
        }
    }
}
