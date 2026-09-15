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

        public IEnumerable<string> LogBuffer => Messages;

        public void Write(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            Messages.Add(message);
            LogReceived?.Invoke(message);
        }
    }
}
