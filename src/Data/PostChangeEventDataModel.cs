using System.Text.Json;

namespace SimpleChattyServer.Data
{
    public sealed class PostChangeEventDataModel : EventDataModel
    {
        public int PostId { get; set; }

        public override void Write(Utf8JsonWriter writer) =>
            JsonSerializer.Serialize(writer, this, _options);
    }
}
