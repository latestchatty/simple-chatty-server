using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateTimeOffset.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var pptTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, PacificTimeZone.TimeZoneId);
            var amPm = $"{pptTime:tt}".ToLowerInvariant();
            var timeZoneAbbreviation = PacificTimeZone.GetAbbreviationFromOffset(pptTime.Offset);
            writer.WriteStringValue($"{pptTime:MMM dd, yyyy H:mm}{amPm} {timeZoneAbbreviation}");
        }
    }
}
