using System;

namespace ValheimServerGUI.Tools
{
    public static class TimeFormats
    {
        public const string DisplayISO = "yyyy-MM-ddTHH:mm:ssZ";
        public const string FilenameISO = "yyyy-MM-dd_HH-mm-ssZ";
        public const string LogPrefix = "[HH:mm:ss.fff]";
        public const string ServerElapsed = @"hh\:mm\:ss";

        public static string ToDisplayISOFormat(this DateTime dateTime)
        {
            return dateTime.ToString(DisplayISO);
        }

        public static string ToFilenameISOFormat(this DateTime dateTime)
        {
            return dateTime.ToString(FilenameISO);
        }

        public static string ToLogPrefixFormat(this DateTimeOffset dateTime)
        {
            return dateTime.ToString(LogPrefix);
        }

        public static string ToServerElapsedFormat(this TimeSpan timeSpan)
        {
            return timeSpan.ToString(ServerElapsed);
        }
    }
}
