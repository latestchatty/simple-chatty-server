using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V2DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue($"{value.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}");
    }
}
