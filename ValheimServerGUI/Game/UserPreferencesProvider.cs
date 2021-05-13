using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Game
{
    public interface IUserPreferencesProvider
    {
        event EventHandler<UserPreferences> PreferencesSaved;

        UserPreferences LoadPreferences();

        void SavePreferences(UserPreferences preferences);
    }

    public class UserPreferencesProvider : JsonFileProvider, IUserPreferencesProvider
    {
        private readonly string UserPrefsFilePath;
        private readonly string LegacyPath;

        private UserPreferences CurrentPreferences;

        public UserPreferencesProvider(ILogger logger) : base(logger)
        {
            this.UserPrefsFilePath = Environment.ExpandEnvironmentVariables(Resources.UserPrefsFilePathV2);
            this.LegacyPath = Environment.ExpandEnvironmentVariables(Resources.UserPrefsFilePath);
        }

        #region IUserPreferencesProvider implementation

        public event EventHandler<UserPreferences> PreferencesSaved;

        public UserPreferences LoadPreferences()
        {
            if (this.CurrentPreferences == null)
            {
                this.CurrentPreferences = LoadInternal();
            }

            return this.CurrentPreferences;
        }

        public void SavePreferences(UserPreferences preferences)
        {
            this.CurrentPreferences = preferences;
            this.SaveInternal(preferences);
            this.PreferencesSaved?.Invoke(this, preferences);
        }

        #endregion

        #region Non-public methods

        private void SaveInternal(UserPreferences preferences)
        {
            try
            {
                var file = preferences.ToFile();
                this.SaveAsync<UserPreferencesFile>(this.UserPrefsFilePath, file).GetAwaiter().GetResult();
                this.Logger.LogInformation("User preferences saved");
            }
            catch (Exception e)
            {
                this.Logger.LogException(e, "Failed to save user preferences");
            }
        }

        public UserPreferences LoadInternal()
        {
            try
            {
                if (!File.Exists(UserPrefsFilePath))
                {
                    if (this.TryMigrateLegacyPrefs(out var legacyPrefs))
                    {
                        return legacyPrefs;
                    }
                    else
                    {
                        return UserPreferences.GetDefault();
                    }
                }

                var file = this.LoadAsync<UserPreferencesFile>(this.UserPrefsFilePath).GetAwaiter().GetResult();
                return UserPreferences.FromFile(file);
            }
            catch (Exception e)
            {
                this.Logger.LogException(e, "Failed to load user preferences");
                return UserPreferences.GetDefault();
            }
        }

        private static readonly Dictionary<string, Action<UserPreferences, string>> MigrationActions = new()
        {
            { "ValheimGamePath", (p, v) => p.ValheimGamePath = v },
            { "ValheimServerPath", (p, v) => p.ValheimServerPath = v },
            { "ServerName", (p, v) => p.ServerName = v },
            { "ServerPassword", (p, v) => p.ServerPassword = v },
            { "ServerWorldName", (p, v) => p.ServerWorldName = v },
            { "ServerPublic", (p, v) => p.ServerPublic = bool.TryParse(v, out var v2) ? v2 : false },
            { "ServerPort", (p, v) => p.ServerPort = int.TryParse(v, out var v2) ? v2 : int.Parse(Resources.DefaultServerPort) },
        };

        private bool TryMigrateLegacyPrefs(out UserPreferences prefs)
        {
            if (!File.Exists(LegacyPath))
            {
                prefs = null;
                return false;
            }

            try
            {
                this.Logger.LogInformation("Migrating userprefs.txt to userprefs.json...");

                prefs = new UserPreferences();

                foreach (var line in File.ReadAllLines(LegacyPath))
                {
                    var parts = line.Split("=");
                    if (parts.Length < 2) continue; // Ignore invalid lines

                    var key = parts[0].Trim();
                    var value = string.Join("=", parts.Skip(1)).Trim();

                    if (MigrationActions.TryGetValue(key, out var action))
                    {
                        action(prefs, value);
                    }
                }

                this.SaveInternal(prefs);
                File.Delete(LegacyPath);

                this.Logger.LogInformation("Migration OK!");

                return true;
            }
            catch (Exception e)
            {
                this.Logger.LogException(e, "Migration failed");
                prefs = null;
                return false;
            }
        }

        #endregion
    }
}
