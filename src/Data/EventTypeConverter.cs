using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class EventTypeConverter : JsonConverter<EventType>
    {
        public override EventType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, EventType value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ToJsonString(value));

        private static string ToJsonString(EventType self)
        {
            switch (self)
            {
                case EventType.NewPost: return "newPost";
                case EventType.CategoryChange: return "categoryChange";
                case EventType.LolCountsUpdate: return "lolCountsUpdate";
                default: throw new ArgumentOutOfRangeException(nameof(self));
            }
        }

        public static EventType Parse(string str)
        {
            switch (str)
            {
                case "newPost": return EventType.NewPost;
                case "categoryChange": return EventType.CategoryChange;
                case "lolCountsUpdate": return EventType.LolCountsUpdate;
                default: throw new ArgumentOutOfRangeException(str);
            }
        }
    }
}
