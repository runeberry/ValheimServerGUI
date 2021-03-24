using Microsoft.Extensions.Logging;
using System;

namespace ValheimServerGUI.Tools.Logging
{
    public class EventLogContext
    {
        public string Message;

        public DateTime Timestamp;

        public string Category;

        public LogLevel LogLevel;

        public EventId EventId;

        public Exception Exception;
    }
}
