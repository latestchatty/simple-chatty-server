using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleChattyServer.Data;
using SimpleChattyServer.Data.Requests;
using SimpleChattyServer.Data.Responses;
using SimpleChattyServer.Services;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SimpleChattyServer.Controllers
{
    [ApiController, Route("chatty")]
    public sealed class V1Controller : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ChattyProvider _chattyProvider;
        private readonly SearchParser _searchParser;
        private readonly FrontPageParser _frontPageParser;
        private readonly MessageParser _messageParser;

        public V1Controller(ILogger<V1Controller> logger, ChattyProvider chattyProvider, SearchParser searchParser,
            FrontPageParser frontPageParser, MessageParser messageParser)
        {
            this._logger = logger;
            _chattyProvider = chattyProvider;
            _searchParser = searchParser;
            _frontPageParser = frontPageParser;
            _messageParser = messageParser;
        }

        [HttpGet("about")]
        public ContentResult About()
        {
            return Content("<h1>SimpleChattyServer</h1>", "text/html");
        }

        [HttpGet("index.json")]
        public V1PageModel IndexJson()
        {
            return GetChattyPage(1);
        }

        [HttpGet("{ignored}.{page}.json")]
        public V1PageModel Chatty(string ignored, int page)
        {
            return GetChattyPage(page);
        }

        [HttpGet("thread/{threadId}.json")]
        public async Task<V1ThreadModel> Thread(int threadId)
        {
            var chattyThread = await _chattyProvider.GetThread(threadId);
            var list = new List<V1RootCommentModel>();
            var maxDepth = chattyThread.Posts.Max(x => x.Depth);
            var lastIdAtDepth = new int[maxDepth + 1];
            var commentsById = new Dictionary<int, V1CommentModel>();
            var op = chattyThread.Posts[0];
            var v1RootCommentModel =
                new V1RootCommentModel
                    {
                        Comments = new List<V1CommentModel>(),
                        ReplyCount = chattyThread.Posts.Count,
                        Body = op.Body,
                        Date = op.Date,
                        Participants = GetParticipants(chattyThread),
                        Category = op.Category,
                        LastReplyId = $"{chattyThread.Posts.Max(x => x.Id)}",
                        Author = op.Author,
                        Preview = ThreadParser.PreviewFromBody(op.Body),
                        Id = $"{op.Id}"
                    };
            foreach (var post in chattyThread.Posts.Skip(1))
            {
                lastIdAtDepth[post.Depth] = post.Id;
                var v1CommentModel =
                    new V1CommentModel
                    {
                        Comments = new List<V1CommentModel>(),
                        Body = post.Body,
                        Date = post.Date,
                        Category = post.Category,
                        Author = post.Author,
                        Preview = ThreadParser.PreviewFromBody(post.Body),
                        Id = $"{post.Id}"
                    };
                commentsById[post.Id] = v1CommentModel;
                if (post.Depth == 1)
                    v1RootCommentModel.Comments.Add(v1CommentModel);
                else
                    commentsById[lastIdAtDepth[post.Depth - 1]].Comments.Add(v1CommentModel);
            }
            return new V1ThreadModel
            {
                Comments = new List<V1RootCommentModel> { v1RootCommentModel }
            };
        }

        [HttpGet("search.json")]
        public async Task<V1SearchResponse> Search(string terms = "", string author = "", string parent_author = "",
            int page = 1)
        {
            var results = await _searchParser.Search(terms, author, parent_author, "", page);
            var lastPage = (int)Math.Ceiling((double)results.TotalResults / 15);
            return new V1SearchResponse
            {
                Terms = terms,
                Author = author,
                ParentAuthor = parent_author,
                LastPage = lastPage,
                Comments = (
                    from result in results.Results
                    select new V1SearchResultModel
                    {
                        Author = result.Author,
                        Date = result.Date,
                        Id = $"{result.Id}",
                        Preview = result.Preview
                    }).ToList()
            };
        }

        [HttpGet("stories.json")]
        public async Task<List<V1StoryPreviewModel>> Stories()
        {
            var list = await _frontPageParser.GetStories();
            return (
                from x in list
                select new V1StoryPreviewModel
                {
                    Body = x.Body,
                    CommentCount = 0,
                    Date = x.Date,
                    Id = x.Id,
                    Name = x.Name,
                    Preview = x.Preview,
                    Url = x.Url,
                    ThreadId = ""
                }).ToList();
        }

        [HttpGet("stories/{storyId}.json")]
        public async Task<V1StoryModel> Story(int storyId)
        {
            return await _frontPageParser.GetArticle(storyId);
        }

        [HttpGet("messages.json")]
        public async Task<V1MessagesResponse> Messages()
        {
            var auth = GetBasicAuthorization();
            if (!auth.HasValue)
                return null;
            var messagePage = await _messageParser.GetMessagePage(Mailbox.Inbox, auth.Value.Username,
                auth.Value.Password, 1);
            var list = (
                from x in messagePage.Messages
                select new V1MessagesResponse.Message
                {
                    Id = $"{x.Id}",
                    From = x.From,
                    To = x.To,
                    Subject = x.Subject,
                    Date = x.Date,
                    Body = x.Body,
                    Unread = x.Unread
                }).ToList();
            return new V1MessagesResponse
            {
                User = auth.Value.Username,
                Messages = list
            };
        }

        [HttpPut("messages/{id}.json")]
        public async Task<ContentResult> MarkMessageRead(int id)
        {
            var auth = GetBasicAuthorization();
            if (!auth.HasValue)
                return Content("", "text/plain");
            await _messageParser.MarkMessageAsRead(auth.Value.Username, auth.Value.Password, id);
            return Content("ok", "text/plain");
        }

        [HttpPost("messages/send")]
        public async Task<ContentResult> SendMessage([FromForm] V1SendMessageRequest request)
        {
            var auth = GetBasicAuthorization();
            if (!auth.HasValue)
                return Content("", "text/plain");
            await _messageParser.SendMessage(auth.Value.Username, auth.Value.Password, request.To, request.Subject,
                request.Body);
            return Content("OK", "text/plain");
        }

        [HttpPost("post")]
        public async Task<ContentResult> Post([FromForm] V1PostRequest request)
        {
            var auth = GetBasicAuthorization();
            if (!auth.HasValue)
                return Content("", "text/plain");
            await _chattyProvider.Post(auth.Value.Username, auth.Value.Password,
                string.IsNullOrWhiteSpace(request.ParentId) ? 0 : int.Parse(request.ParentId),
                request.Body);
            return Content("", "text/plain");
        }

        private (string Username, string Password)? GetBasicAuthorization()
        {
            if (Request.Headers.TryGetValue("Authorization", out var headerValue))
            {
                var auth = AuthenticationHeaderValue.Parse(headerValue);
                if (auth.Scheme == AuthenticationSchemes.Basic.ToString())
                {
                    var credentials = Encoding.UTF8
                        .GetString(Convert.FromBase64String(auth.Parameter ?? ""))
                        .Split(':', 2);
                    if (credentials.Length == 2)
                        return (credentials[0], credentials[1]);
                }
            }

            Response.StatusCode = 401;
            Response.Headers["WWW-Authenticate"] = "Basic realm=\"Shacknews\", charset=\"UTF-8\"";
            return null;
        }

        private V1PageModel GetChattyPage(int page)
        {
            var chatty = _chattyProvider.GetChatty();
            var threadsPerPage = 40;
            var lastPage = (int)Math.Ceiling((double)chatty.Threads.Count / threadsPerPage);
            var list = new List<V1RootCommentModel>(threadsPerPage);
            foreach (var chattyThread in
                chatty.Threads
                .Skip((page - 1) * threadsPerPage)
                .Take(threadsPerPage))
            {
                var op = chattyThread.Posts[0];
                list.Add(
                    new V1RootCommentModel
                    {
                        Comments = new List<V1CommentModel>(),
                        ReplyCount = chattyThread.Posts.Count,
                        Body = op.Body,
                        Date = op.Date,
                        Participants = GetParticipants(chattyThread),
                        Category = op.Category,
                        LastReplyId = $"{chattyThread.Posts.Max(x => x.Id)}",
                        Author = op.Author,
                        Preview = ThreadParser.PreviewFromBody(op.Body),
                        Id = $"{op.Id}"
                    });
            }

            return new V1PageModel
            {
                Comments = list,
                Page = $"{page}",
                LastPage = lastPage
            };
        }

        private static List<V1ParticipantModel> GetParticipants(ChattyThread chattyThread) => (
            from post in chattyThread.Posts
            group post by post.Author into authorGroup
            select new V1ParticipantModel
            {
                Username = authorGroup.Key,
                PostCount = authorGroup.Count()
            }).ToList();
    }
}
