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

        public UserPreferencesProvider(ILogger logger) : base(logger)
        {
            UserPrefsFilePath = Environment.ExpandEnvironmentVariables(Resources.UserPrefsFilePathV2);
            LegacyPath = Environment.ExpandEnvironmentVariables(Resources.UserPrefsFilePath);
        }

        #region IUserPreferencesProvider implementation

        public event EventHandler<UserPreferences> PreferencesSaved;

        public UserPreferences LoadPreferences()
        {
            return LoadInternal();
        }

        public void SavePreferences(UserPreferences preferences)
        {
            SaveInternal(preferences);
        }

        #endregion

        #region System Events

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath != UserPrefsFilePath) return;

            var prefs = LoadInternal();
            PreferencesSaved?.Invoke(this, prefs);
        }

        #endregion

        #region Non-public methods

        private void SaveInternal(UserPreferences preferences)
        {
            try
            {
                var file = preferences.ToFile();
                SaveAsync(UserPrefsFilePath, file).GetAwaiter().GetResult();
                Logger.LogInformation("User preferences saved");
            }
            catch (Exception e)
            {
                Logger.LogException(e, "Failed to save user preferences");
                return;
            }

            PreferencesSaved?.Invoke(this, preferences);
        }

        private UserPreferences LoadInternal()
        {
            try
            {
                if (!File.Exists(UserPrefsFilePath))
                {
                    if (TryMigrateLegacyPrefs(out var legacyPrefs))
                    {
                        return legacyPrefs;
                    }
                    else
                    {
                        return UserPreferences.GetDefault();
                    }
                }

                var file = LoadAsync<UserPreferencesFile>(UserPrefsFilePath).GetAwaiter().GetResult();
                return UserPreferences.FromFile(file);
            }
            catch (Exception e)
            {
                Logger.LogException(e, "Failed to load user preferences");
                return UserPreferences.GetDefault();
            }
        }

        private static readonly Dictionary<string, Action<UserPreferences, string>> MigrationActions = new()
        {
            //{ "ValheimGamePath", (p, v) => p.ValheimGamePath = v },
            { "ValheimServerPath", (p, v) => p.ServerExePath = v },
            { "ServerName", (p, v) => p.Servers[0].Name = v },
            { "ServerPassword", (p, v) => p.Servers[0].Password = v },
            { "ServerWorldName", (p, v) => p.Servers[0].WorldName = v },
            { "ServerPublic", (p, v) => p.Servers[0].Public = bool.TryParse(v, out var v2) ? v2 : false },
            { "ServerPort", (p, v) => p.Servers[0].Port = int.TryParse(v, out var v2) ? v2 : int.Parse(Resources.DefaultServerPort) },
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
                Logger.LogInformation("Migrating userprefs.txt to userprefs.json...");

                prefs = new UserPreferences();

                foreach (var line in File.ReadAllLines(LegacyPath))
                {
                    var parts = line.Split("=");
                    if (parts.Length < 2) continue; // Ignore invalid lines

                    var key = parts[0].Trim();
                    var value = string.Join("=", parts.Skip(1)).Trim();

                    if (MigrationActions.TryGetValue(key, out var action))
                    {
                        if (prefs.Servers.Count == 0)
                        {
                            prefs.Servers.Add(new ServerPreferences());
                        }

                        action(prefs, value);
                    }
                }

                SaveInternal(prefs);
                File.Delete(LegacyPath);

                Logger.LogInformation("Migration OK!");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e, "Migration failed");
                prefs = null;
                return false;
            }
        }

        #endregion
    }
}
