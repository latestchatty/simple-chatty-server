using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1ParticipantModel
    {
        public string Username { get; set; }
        [JsonPropertyName("post_count")] public int PostCount { get; set; }
    }
}
