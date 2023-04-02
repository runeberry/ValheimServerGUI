using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<LogEventLevel, string> LevelPrefixes = new()
        {
            { LogEventLevel.Verbose, "[VER] " },
            { LogEventLevel.Debug, "[DBG] " },
            { LogEventLevel.Information, "" },
            { LogEventLevel.Warning, "[WRN] " },
            { LogEventLevel.Error, "[ERR] " },
            { LogEventLevel.Fatal, "[FAT] " },
        };

        public ApplicationLogger(IServiceProvider services)
        {
            // Dependencies are injected late to avoid creating a circular dependency
            ServiceProvider = services;

            AddModifier((evt, message) => $"{LevelPrefixes[evt.Level]}{message}");
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
