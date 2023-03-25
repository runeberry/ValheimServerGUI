using Serilog;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IApplicationLogger : IBaseLogger
    {
    }

    public class ApplicationLogger : BaseLogger, IApplicationLogger
    {
        public ApplicationLogger()
        {
            SetFileLoggingEnabled(true); // todo: use prefs
        }

        #region BaseLogger overrides

        protected override string LogFileName => "ApplicationLogs";

        protected override void ConfigureLogger(LoggerConfiguration config)
        {
            // no-op
        }

        #endregion
    }
}
