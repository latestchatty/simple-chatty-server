using System;
using System.Runtime.InteropServices;

namespace SimpleChattyServer.Data
{
    public static class PacificTimeZone
    {
        public static string TimeZoneId =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Pacific Standard Time"
            : "America/Los_Angeles";

        public static string GetAbbreviationFromOffset(TimeSpan offset) =>
            offset.Hours == -7 ? "PDT" :
            offset.Hours == -8 ? "PST" :
            "UTC";

        public static TimeSpan GetOffsetFromAbbreviation(string abbreviation) =>
            abbreviation == "PDT" ? TimeSpan.FromHours(-7) :
            abbreviation == "PST" ? TimeSpan.FromHours(-8) :
            TimeSpan.FromHours(0);
    }
}
