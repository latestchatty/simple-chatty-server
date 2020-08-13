using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class MarkedPostModel
    {
        public int Id { get; set; }
        [JsonConverter(typeof(MarkedPostTypeConverter))] public MarkedPostType Type { get; set; }
    }
}
