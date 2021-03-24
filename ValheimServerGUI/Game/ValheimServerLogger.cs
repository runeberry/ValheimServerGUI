using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Game
{
    public class ValheimServerLogger : EventLogger<ValheimServer>
    {
        private static readonly List<string> FilteredMessages = new List<string>
        {
            @"^\(Filename:",
        };

        protected override bool FilterLog(EventLogContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Message)) return false;
            if (FilteredMessages.Any(f => Regex.IsMatch(context.Message, f))) return false;

            return true;
        }

        protected override string FormatLog(EventLogContext context)
        {
            // Not all log message have timestamps, but trim off any that do
            var message = Regex.Replace(context.Message, @"^\d+\/\d+\/\d+ \d+:\d+:\d+:\s+", "");

            // Add a consistent timestamp to the beginning of all messages
            message = $"[{context.Timestamp:T}] {message}";

            return message;
        }
    }
}
