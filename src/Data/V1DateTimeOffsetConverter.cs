using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Data
{
    public sealed class V1DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
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
            var timeZoneAbbreviation = 
			    pptTime.Offset.Hours == -7 ? "PDT" :
			    pptTime.Offset.Hours == -8 ? "PST" :
			    throw new Api500Exception(Api500Exception.Codes.SERVER, "Misconfigured server time zones. The server may be missing the ICU library.");
            writer.WriteStringValue($"{pptTime:MMM dd, yyyy H:mm}{amPm} {timeZoneAbbreviation}");
        }
    }
}
