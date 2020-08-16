using System.Text.Json;

namespace SimpleChattyServer.Data
{
    public sealed class ReadStatusUpdateEventDataModel : EventDataModel
    {
        public string Username { get; set; }

        public override void Write(Utf8JsonWriter writer) =>
            JsonSerializer.Serialize(writer, this, _options);
    }
}
