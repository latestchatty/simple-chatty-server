using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class PostModel
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public int ParentId { get; set; }
        public string Author { get; set; }
        [JsonConverter(typeof(V2ModerationFlagConverter))] public ModerationFlag Category { get; set; }
        [JsonConverter(typeof(V2DateTimeOffsetConverter))] public DateTimeOffset Date { get; set; }
        public string Body { get; set; }
        public List<LolModel> Lols { get; set; }

        public static List<PostModel> CreateList(ChattyThread thread, ThreadLolCounts lolCounts)
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
