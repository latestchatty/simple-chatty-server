using System;
using System.Net;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class ChattyProvider
    {
        private readonly ThreadParser _threadParser;
        private readonly LolParser _lolParser;
        private readonly DownloadService _downloadService;
        private readonly EmojiConverter _emojiConverter;

        public Chatty Chatty { get; private set; }
        public ChattyLolCounts LolCounts { get; private set; }

        public ChattyProvider(ThreadParser threadParser, LolParser lolParser, DownloadService downloadService,
            EmojiConverter emojiConverter)
        {
            _threadParser = threadParser;
            _lolParser = lolParser;
            _downloadService = downloadService;
            _emojiConverter = emojiConverter;
        }

        public void Update(Chatty chatty, ChattyLolCounts lolCounts)
        {
            Chatty = chatty;
            LolCounts = lolCounts;
        }

        public Chatty GetChatty()
        {
            var chatty = Chatty;
            if (chatty == null)
                throw new Api500Exception(Api500Exception.Codes.SERVER,
                    "The server has just started up and has not yet downloaded the chatty.");
            
            return chatty;
        }

        public ChattyLolCounts GetChattyLolCounts() =>
            LolCounts ?? ChattyLolCounts.Empty;

        public async Task<(ChattyThread, ThreadLolCounts)> GetThreadAndLols(int postId)
        {
            var chatty = Chatty;

            ChattyThread thread;
            ThreadLolCounts threadLolCounts;
            if (chatty != null && chatty.ThreadsByReplyId.TryGetValue(postId, out thread))
            {
                threadLolCounts =
                    LolCounts?.GetThreadLolCounts(thread.Posts[0].Id)
                    ?? ThreadLolCounts.Empty;
            }
            else
            {
                thread = await _threadParser.GetThread(postId);
                threadLolCounts = await _lolParser.DownloadThreadLolCounts(thread);
            }

            return (thread, threadLolCounts);
        }

        public async Task<ChattyThread> GetThread(int postId)
        {
            var chatty = Chatty;
            if (chatty != null && chatty.ThreadsByReplyId.TryGetValue(postId, out var thread))
                return thread;
            else
                return await _threadParser.GetThread(postId);
        }

        public async Task<ChattyThread> GetThreadTree(int postId)
        {
            var chatty = Chatty;
            if (chatty != null && chatty.ThreadsByReplyId.TryGetValue(postId, out var thread))
                return thread;
            else
                return await _threadParser.GetThreadTree(postId);
        }

        public async Task Post(string username, string password, int parentId, string body)
        {
            if (body.Length > 0 && body[0] == '@')
                body = " " + body;

            var contentTypeId = 17;
            var contentId = 17;

            if (parentId != 0)
            {
                try
                {
                    (contentTypeId, contentId) = await _threadParser.GetContentTypeId(parentId);
                }
                catch (ParsingException)
                {
                    throw new Api400Exception(Api400Exception.Codes.INVALID_PARENT, "Invalid parent ID.");
                }
            }

            var query = _downloadService.NewQuery();
            query.Add("parent_id", parentId == 0 ? "" : $"{parentId}");
            query.Add("content_type_id", $"{contentTypeId}");
            query.Add("content_id", $"{contentId}");
            query.Add("page", "");
            query.Add("parent_url", "/chatty");
            query.Add("body", _emojiConverter.ConvertEmojisToEntities(body));
            
            var response = await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/post_chatty.x",
                username, password, query.ToString());
            
            await Task.Delay(TimeSpan.FromSeconds(10));

            if (response.Contains("You must be logged in to post"))
                throw new Api400Exception(Api400Exception.Codes.INVALID_LOGIN,
                    "Unable to log into user account.");
            if (response.Contains("Please wait a few minutes before trying to post again"))
                throw new Api400Exception(Api400Exception.Codes.POST_RATE_LIMIT,
                    "Please wait a few minutes before trying to post again.");            
            if (response.Contains("banned"))
                throw new Api400Exception(Api400Exception.Codes.BANNED,
                    "You are banned.");
            if (response.Contains("Trying to post to a nuked thread"))
                throw new Api400Exception(Api400Exception.Codes.NUKED,
                    "You cannot reply to a nuked thread or subthread.");
            if (!response.Contains("fixup_postbox_parent_for_remove("))
                throw new Api500Exception("Unexpected response from server: " + response);
        }

        public async Task SetPostCategory(string username, string password, int postId, ModerationFlag category)
        {
            var thread = await GetThread(postId);

            int categoryInt;
            switch (category)
            {
                case ModerationFlag.OnTopic: categoryInt = 5; break;
                case ModerationFlag.Nws: categoryInt = 2; break;
                case ModerationFlag.Stupid: categoryInt = 3; break;
                case ModerationFlag.Political: categoryInt = 9; break;
                case ModerationFlag.Tangent: categoryInt = 4; break;
                case ModerationFlag.Informative: categoryInt = 1; break;
                case ModerationFlag.Nuked: categoryInt = 8; break;
                default: throw new Api400Exception("Unexpected category string.");
            }

            var query = _downloadService.NewQuery();
            query.Add("root", $"{thread.ThreadId}");
            query.Add("post_id", $"{postId}");
            query.Add("mod_type_id", $"{categoryInt}");

            var response = await _downloadService.DownloadWithUserLogin(
                "https://www.shacknews.com/mod_chatty.x?" + query.ToString(),
                username, password);
            if (response.Contains("Invalid moderation flags"))
                throw new Api500Exception("Possible bug in the API. Server does not understand the moderation flag.");
            if (!response.Contains("navigate_page_no_history( window, \"/frame_chatty.x?root="))
                throw new Api400Exception(Api400Exception.Codes.NOT_MODERATOR,
                    "Failed to set the post category. User likely does not have moderator privileges.");
        }
    }
}
