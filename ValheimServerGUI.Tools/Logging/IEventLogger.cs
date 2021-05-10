using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IEventLogger : ILogger
    {
        IEnumerable<string> LogBuffer { get; }

        event EventHandler<EventLogContext> LogReceived;
    }

    public interface IEventLogger<TCategoryName> : IEventLogger
    {
    }
}
