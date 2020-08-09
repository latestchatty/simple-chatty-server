using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            throw new NotImplementedException(); //TODO
        }
    }
}
