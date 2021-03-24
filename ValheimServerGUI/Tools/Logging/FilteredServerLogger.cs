using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ValheimServerGUI.Tools.Logging
{
    public class FilteredServerLogger : AppLogger
    {
        private static readonly List<string> FilteredMessages = new List<string>
        {
            @"^\(Filename:",
        };

        protected override bool FilterLogEvent(LogEvent logEvent)
        {
            if (string.IsNullOrWhiteSpace(logEvent.Message)) return false;
            if (FilteredMessages.Any(f => Regex.IsMatch(logEvent.Message, f))) return false;

            return true;
        }

        protected override void FormatLogEvent(LogEvent logEvent)
        {
            // Not all log message have timestamps, but trim off any that do
            logEvent.Message = Regex.Replace(logEvent.Message, @"^\d+\/\d+\/\d+ \d+:\d+:\d+:\s+", "");

            // Add a consistent timestamp to the beginning of all messages
            logEvent.Message = $"[{logEvent.Timestamp:T}] {logEvent.Message}";
        }
    }
}
