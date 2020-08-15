using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1MessageDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var pptTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, PacificTimeZone.TimeZoneId);
            var amPm = $"{pptTime:tt}".ToLowerInvariant();
            var timeZoneAbbreviation = PacificTimeZone.GetAbbreviationFromOffset(pptTime.Offset);
            writer.WriteStringValue($"{pptTime:MMMM d, yyyy, h:mm} {amPm} {timeZoneAbbreviation}");
        }
    }
}
