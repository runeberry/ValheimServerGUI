using Microsoft.Extensions.Logging;
using System;

namespace ValheimServerGUI.Tools.Logging
{
    public class LogEvent
    {
        public LogLevel LogLevel { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string EventId { get; set; }

        public string Message { get; set; }
    }
}
