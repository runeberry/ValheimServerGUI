using System;
using System.Collections.Generic;
using Serilog.Events;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>Recording application logger for headless tests: keeps every rendered message.</summary>
    public class FakeApplicationLogger : IApplicationLogger
    {
        public event Action<string>? LogReceived;

        public List<string> Messages { get; } = new();

        /// <summary>Every event as written (level, exception, properties), alongside <see cref="Messages"/>.</summary>
        public List<LogEvent> Events { get; } = new();

        public IEnumerable<string> LogBuffer => Messages;

        public void Write(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            Events.Add(logEvent);
            Messages.Add(message);
            LogReceived?.Invoke(message);
        }
    }
}
