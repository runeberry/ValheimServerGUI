using Serilog.Events;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public class LogLevelTransformer : LogTransformer
    {
        private static readonly Dictionary<LogEventLevel, string> LevelPrefixes = new()
        {
            { LogEventLevel.Verbose, "[VER] " },
            { LogEventLevel.Debug, "[DBG] " },
            { LogEventLevel.Information, "" },
            { LogEventLevel.Warning, "[WRN] " },
            { LogEventLevel.Error, "[ERR] " },
            { LogEventLevel.Fatal, "[FAT] " },
        };

        public static readonly LogLevelTransformer Default = new();

        public override string Transform(LogEvent logEvent, string renderedMessage)
        {
            return $"{LevelPrefixes[logEvent.Level]}{renderedMessage}";
        }
    }
}
