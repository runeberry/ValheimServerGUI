using Serilog;
using System.Text.RegularExpressions;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Logging.Components;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IValheimServerLogger : IBaseLogger
    {
    }

    public class ValheimServerLogger : BaseLogger, IValheimServerLogger
    {
        private readonly IValheimServerOptions Options;

        public ValheimServerLogger(IValheimServerOptions options)
        {
            Options = options;

            // Remove default timestamp on some logs
            AddModifier((_, message) => Regex.Replace(message, @"^\d+\/\d+\/\d+ \d+:\d+:\d+:\s+", ""));
        }

        #region DynamicLogger overrides

        protected override void ConfigureLogger(LoggerConfiguration config)
        {
            if (Options.LogToFile) AddFileLogging(config, $"ServerLogs-{Options.Name}");

            // Ignore Unity debug logs
            config.AddRegexExclusion(@"^\(Filename:");
        }

        #endregion
    }
}
