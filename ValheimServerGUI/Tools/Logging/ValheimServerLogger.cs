using Serilog;
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

            // Remove default timestamp when it's present on a log
            AddRule(RegexTransformer.Remove(@"^\d+\/\d+\/\d+ \d+:\d+:\d+:\s+"));

            if (!Options.LogFilteringDisabled)
            {
                // Ignore excess Unity logs
                AddRule(RegexFilter.Exclude(@"^\(Filename:"));
                AddRule(RegexFilter.Exclude(@"^Console: "));

                // Ignore empty lines
                AddRule(RegexFilter.Exclude(@"^\s*?$"));
            }

            // Add a timestamp after filtering
            AddRule(TimestampTransformer.Default);
        }

        #region BaseLogger overrides

        protected override void ConfigureLogger(LoggerConfiguration config)
        {
            if (Options.LogToFile) AddFileLogging(config, $"ServerLogs-{Options.Name}");
        }

        #endregion
    }
}
