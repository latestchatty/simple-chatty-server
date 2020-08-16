using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class EventTypeConverter : JsonConverter<EventType>
    {
        public override EventType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, EventType value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToJsonString(value));

        private static string ToJsonString(EventType self)
        {
            switch (self)
            {
                case EventType.NewPost: return "newPost";
                case EventType.CategoryChange: return "categoryChange";
                case EventType.LolCountsUpdate: return "lolCountsUpdate";
                case EventType.ReadStatusUpdate: return "readStatusUpdate";
                case EventType.PostChange: return "postChange";
                default: throw new ArgumentOutOfRangeException(nameof(self));
            }
        }
    }
}
