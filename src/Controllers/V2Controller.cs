using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SimpleChattyServer.Data;
using SimpleChattyServer.Responses;
using SimpleChattyServer.Services;

namespace SimpleChattyServer.Controllers
{
    [ApiController, Route("v2")]
    public sealed class V2Controller : ControllerBase
    {
        private readonly ChattyAccess _chattyAccess;

        public V2Controller(ChattyAccess chattyAccess)
        {
            _chattyAccess = chattyAccess;
        }

        [HttpGet("getChatty")]
        public GetChattyResponse GetChatty()
        {
            var chatty = _chattyAccess.GetChatty();
            var lolCounts = _chattyAccess.GetChattyLolCounts();
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
                        Category = V2ModerationFlagConverter.Parse(post.Category),
                        Date = post.Date,
                        Body = post.Body,
                        Lols =
                            lolCounts.CountsByPostId.TryGetValue(post.Id, out var postLols)
                            ? postLols
                            : new List<LolModel>()
                    }
                );
            }

            return list;
        }
    }
}
