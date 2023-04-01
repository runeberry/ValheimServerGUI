using Serilog;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IApplicationLogger : IBaseLogger
    {
    }

    public class ApplicationLogger : BaseLogger, IApplicationLogger
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        public ApplicationLogger(IUserPreferencesProvider userPrefsProvider)
        {
            UserPrefsProvider = userPrefsProvider;

            var prefs = UserPrefsProvider.LoadPreferences();
            SetFileLoggingEnabled(prefs.WriteApplicationLogsToFile);

            UserPrefsProvider.PreferencesSaved += OnUserPreferencesSaved;
        }

        private void OnUserPreferencesSaved(object sender, UserPreferences prefs)
        {
            SetFileLoggingEnabled(prefs.WriteApplicationLogsToFile);
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
