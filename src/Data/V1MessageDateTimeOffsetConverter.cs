using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1MessageDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return Parse(str);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var pptTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, PacificTimeZone.TimeZoneId);
            var amPm = $"{pptTime:tt}".ToLowerInvariant();
            writer.WriteStringValue($"{pptTime:MMMM dd, yyyy, H:mm} {amPm}");
        }

        public static DateTimeOffset Parse(string str)
        {
            var dateTime = DateTime.ParseExact(str, "MMMM dd, yyyy, H:mm tt", null);
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(PacificTimeZone.TimeZoneId);
            var offset = timeZoneInfo.GetUtcOffset(dateTime);
            return new DateTimeOffset(dateTime, offset);
        }
    }
}
