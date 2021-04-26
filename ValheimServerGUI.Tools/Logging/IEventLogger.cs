using Microsoft.Extensions.Logging;
using System;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IEventLogger : ILogger
    {
        event EventHandler<EventLogContext> LogReceived;
    }

    public interface IEventLogger<TCategoryName> : IEventLogger
    {
    }
}
