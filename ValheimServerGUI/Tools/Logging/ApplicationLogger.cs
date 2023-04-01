using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IApplicationLogger : IBaseLogger
    {
    }

    public class ApplicationLogger : BaseLogger, IApplicationLogger
    {
        private readonly IServiceProvider ServiceProvider;
        private IUserPreferencesProvider UserPrefsProvider;

        public ApplicationLogger(IServiceProvider services)
        {
            // Dependencies are injected late to avoid creating a circular dependency
            ServiceProvider = services;
        }

        private void OnUserPreferencesSaved(object sender, UserPreferences prefs)
        {
            RebuildLogger();
        }

        #region BaseLogger overrides

        protected override void ConfigureLogger(LoggerConfiguration config)
        {
            if (UserPrefsProvider == null)
            {
                UserPrefsProvider = ServiceProvider.GetRequiredService<IUserPreferencesProvider>();
                UserPrefsProvider.PreferencesSaved += OnUserPreferencesSaved;
            }

            var prefs = UserPrefsProvider.LoadPreferences();
            if (prefs.WriteApplicationLogsToFile) AddFileLogging(config, "ApplicationLogs");
        }

        #endregion
    }
}
