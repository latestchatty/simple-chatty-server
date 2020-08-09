using System;
using System.Runtime.InteropServices;
using SimpleChattyServer.Exceptions;

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
            throw new Api500Exception(Api500Exception.Codes.SERVER, $"Time zone offset {offset} is neither PDT nor PST.");

        public static TimeSpan GetOffsetFromAbbreviation(string abbreviation) =>
            abbreviation == "PDT" ? TimeSpan.FromHours(-7) :
            abbreviation == "PST" ? TimeSpan.FromHours(-8) :
            throw new Api500Exception(Api500Exception.Codes.SERVER, $"Time zone {abbreviation} must be either PDT or PST.");
    }
}
