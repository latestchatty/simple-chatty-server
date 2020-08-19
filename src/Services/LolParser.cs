using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SimpleChattyServer.Data;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class LolParser
    {
        private readonly DownloadService _downloadService;

        public LolParser(DownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public async Task<(string Json, ChattyLolCounts LolCounts)> DownloadChattyLolCounts(
            string previousJson = null, ChattyLolCounts previousChattyLolCounts = null)
        {
            var json = await _downloadService.DownloadWithSharedLogin(
                "https://www.shacknews.com/api2/api-index.php?action2=ext_get_counts",
                verifyLoginStatus: false);
            if (json == previousJson)
                return (json, previousChattyLolCounts);

            var response = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
                json);
            return (json,
                new ChattyLolCounts
                {
                    CountsByThreadId = (
                        from threadPair in response
                        let threadId = int.Parse(threadPair.Key)
                        let threadTagsDict = (
                            from postPair in threadPair.Value
                            let postId = int.Parse(postPair.Key)
                            let postLols = (
                                from x in postPair.Value
                                select new LolModel { Tag = x.Key, Count = int.Parse(x.Value) }
                                ).ToList()
                            select (PostId: postId, PostTags: postLols)
                            ).ToDictionary(x => x.PostId, x => x.PostTags)
                        let threadLolCounts = new ThreadLolCounts { CountsByPostId = threadTagsDict }
                        select (ThreadId: threadId, ThreadLolCounts: threadLolCounts)
                        ).ToDictionary(x => x.ThreadId, x => x.ThreadLolCounts)
                });
        }

        public async Task<ThreadLolCounts> DownloadThreadLolCounts(ChattyThread thread)
        {
            var query = _downloadService.NewQuery();
            foreach (var post in thread.Posts)
                query.Add("ids[]", $"{post.Id}");

            var json = await _downloadService.DownloadWithSharedLogin(
                "https://www.shacknews.com/api2/api-index.php?action2=get_all_tags_for_posts",
                verifyLoginStatus: false,
                postBody: query.ToString());
                
            var response = JsonSerializer.Deserialize<TagsForPosts>(json,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return
                new ThreadLolCounts
                {
                    CountsByPostId = (
                        from tag in response.Data
                        group tag by int.Parse(tag.ThreadId) into post_group
                        let postLols = (
                            from x in post_group
                            select new LolModel { Tag = GetTagName(x.Tag), Count = int.Parse(x.Total) }
                            ).ToList()
                        select (PostId: post_group.Key, Tags: postLols)
                        ).ToDictionary(x => x.PostId, x => x.Tags)
                };
        }

        private static string GetTagName(string tagNum)
        {
            switch (tagNum)
            {
                case "1": return "lol";
                case "4": return "inf";
                case "3": return "unf";
                case "5": return "tag";
                case "2": return "wtf";
                case "6": return "wow";
                case "7": return "aww";
                default: throw new ParsingException($"Invalid tag number: {tagNum}");
            }
        }

        private sealed class TagsForPosts
        {
            public string Status { get; set; }
            public List<Post> Data { get; set; }

            public sealed class Post
            {
                [JsonPropertyName("thread_id")] public string ThreadId { get; set; }
                public string Tag { get; set; }
                public string Total { get; set; }
            }
        }
    }
}
