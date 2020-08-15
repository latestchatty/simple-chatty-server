using System;
using System.Globalization;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Data
{
    public static class DateParser
    {
        public static DateTimeOffset Parse(string str)
        {
            if (str.EndsWith("PDT") || str.EndsWith("PST"))
            {
                // like "Aug 09, 2020 9:33am PDT"
                var timeZoneAbbreviation = str.Substring(str.Length - 3);
                var timeZoneOffset = PacificTimeZone.GetOffsetFromAbbreviation(timeZoneAbbreviation);
                var reformattedDate = str.Substring(0, str.Length - 3) + $"{timeZoneOffset.Hours}:00";
                return DateTimeOffset.Parse(reformattedDate);
            }
            else if (DateTime.TryParseExact(str, "MMMM d, yyyy, h:mm tt", null, DateTimeStyles.None,
                out var dateTime))
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(PacificTimeZone.TimeZoneId);
                var offset = timeZoneInfo.GetUtcOffset(dateTime);
                return new DateTimeOffset(dateTime, offset);
            } 
            else if (
                DateTimeOffset.TryParseExact(str, "ddd, dd MMM yyyy HH:mm:ss zzzz", null, DateTimeStyles.None,
                    out var dateTimeOffset) // Fri, 14 Aug 2020 21:30:00 -0700
                || DateTimeOffset.TryParseExact(str, "yyyy-MM-ddTHH:mm:sszzzz", null, DateTimeStyles.None,
                    out dateTimeOffset)) // 2020-08-14T14:30:00-07:00
            {
                return dateTimeOffset;
            }
            else
            {
                throw new Api500Exception($"Cannot parse date \"{str}\".");
            }
        }
    }
}
