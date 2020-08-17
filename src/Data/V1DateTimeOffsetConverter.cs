using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(ToJsonString(value));
        }

        public static string ToJsonString(DateTimeOffset value)
        {
            var pptTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, PacificTimeZone.TimeZoneId);
            var amPm = $"{pptTime:tt}".ToLowerInvariant();
            var timeZoneAbbreviation = PacificTimeZone.GetAbbreviationFromOffset(pptTime.Offset);
            string str = $"{pptTime:MMM dd, yyyy h:mm}{amPm} {timeZoneAbbreviation}";
            return str;
        }
    }
}
