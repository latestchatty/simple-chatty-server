using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1ThreadModel
    {
        public List<V1RootCommentModel> Comments { get; set; }
        public int Page { get; set; } = 1;
        [JsonPropertyName("last_page")] public int LastPage { get; set; } = 1;
        [JsonPropertyName("story_id")] public int StoryId { get; set; } = 0;
        [JsonPropertyName("story_name")] public string StoryName { get; set; } = "Latest Chatty";
    }
}
