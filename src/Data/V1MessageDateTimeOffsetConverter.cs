using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleChattyServer.Data
{
    public sealed class V1MessageDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateTimeOffset.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var timeZoneId =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Pacific Standard Time"
                : "America/Los_Angeles";
            var pptTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, timeZoneId);
            var amPm = $"{pptTime:tt}".ToLowerInvariant();
            writer.WriteStringValue($"{pptTime:MMMM dd, yyyy, H:mm} {amPm}");
        }
    }
}
