using System.Text.Json;

namespace SimpleChattyServer.Data
{
    public abstract class EventDataModel
    {
        public abstract void Write(Utf8JsonWriter writer);

        protected static readonly JsonSerializerOptions _options =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
    }
}
