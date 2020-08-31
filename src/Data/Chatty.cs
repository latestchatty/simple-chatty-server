using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class Chatty
    {
        public List<ChattyThread> Threads { get; set; }
        [JsonIgnore] public Dictionary<int, ChattyThread> ThreadsByRootId { get; set; }
        [JsonIgnore] public Dictionary<int, ChattyThread> ThreadsByReplyId { get; set; }
        [JsonIgnore] public Dictionary<int, ChattyPost> PostsById { get; set; }
        [JsonIgnore] public HashSet<int> ExpiredThreadIds { get; set; } = new HashSet<int>();
        [JsonIgnore] public HashSet<int> NukedThreadIds { get; set; } = new HashSet<int>();

        public void SetDictionaries()
        {
            ThreadsByRootId = new Dictionary<int, ChattyThread>(200);
            ThreadsByReplyId = new Dictionary<int, ChattyThread>(2000);
            PostsById = new Dictionary<int, ChattyPost>(2000);

            foreach (var thread in Threads)
            {
                ThreadsByRootId[thread.ThreadId] = thread;

                foreach (var post in thread.Posts)
                {
                    ThreadsByReplyId[post.Id] = thread;
                    PostsById[post.Id] = post;
                }
            }
        }
    }
}
