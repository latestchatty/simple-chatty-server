using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class EventDataModelConverter : JsonConverter<EventDataModel>
    {
        public override EventDataModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, EventDataModel value, JsonSerializerOptions options) =>
            value.Write(writer);
    }
}
