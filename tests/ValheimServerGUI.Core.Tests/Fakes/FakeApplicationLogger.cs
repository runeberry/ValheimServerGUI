using System;
using System.Collections.Generic;
using Serilog.Events;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>No-op application logger for headless tests (drops everything).</summary>
    public class FakeApplicationLogger : IApplicationLogger
    {
        public event Action<string>? LogReceived;

        public IEnumerable<string> LogBuffer => Array.Empty<string>();

        public void Write(LogEvent logEvent)
        {
            // no-op; keep the compiler from warning about an unused event
            LogReceived?.Invoke(logEvent.RenderMessage());
        }
    }
}
