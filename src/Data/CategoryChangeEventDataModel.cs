using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class CategoryChangeEventDataModel : EventDataModel
    {
        public int PostId { get; set; }
        [JsonConverter(typeof(V2ModerationFlagConverter))] public ModerationFlag Category { get; set; }
    }
}
