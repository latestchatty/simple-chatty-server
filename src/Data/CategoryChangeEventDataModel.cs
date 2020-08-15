using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class CategoryChangeEventDataModel : EventDataModel
    {
        public int PostId { get; set; }
        [JsonConverter(typeof(V2ModerationFlagConverter))] public ModerationFlag Category { get; set; }

        public override void Write(Utf8JsonWriter writer) =>
            JsonSerializer.Serialize(writer, this, _options);
    }
}
