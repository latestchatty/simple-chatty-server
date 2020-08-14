using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1PageModel
    {
        public List<V1RootCommentModel> Comments { get; set; }
        public string Page { get; set; }
        [JsonPropertyName("last_page")] public int LastPage { get; set; }
        [JsonPropertyName("story_id")] public int StoryId { get; set; } = 0;
        [JsonPropertyName("story_name")] public string StoryName { get; set; } = "Latest Chatty";
    }
}
