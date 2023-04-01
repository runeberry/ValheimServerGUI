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
        private readonly string ServerName;

        public ValheimServerLogger(IValheimServerOptions options)
        {
            ServerName = options.Name;

            // Remove default timestamp on some logs
            AddModifier((_, message) => Regex.Replace(message, @"^\d+\/\d+\/\d+ \d+:\d+:\d+:\s+", ""));

            SetFileLoggingEnabled(options.LogToFile);
        }

        #region DynamicLogger overrides

        protected override string LogFileName => $"ServerLogs-{ServerName}";

        protected override void ConfigureLogger(LoggerConfiguration config)
        {
            // Ignore Unity debug logs
            config.AddRegexExclusion(@"^\(Filename:");
        }

        #endregion
    }
}
