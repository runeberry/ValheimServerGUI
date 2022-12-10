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

        private FileSystemWatcher FileSystemWatcher;

        public UserPreferencesProvider(ILogger logger) : base(logger)
        {
            this.UserPrefsFilePath = Environment.ExpandEnvironmentVariables(Resources.UserPrefsFilePathV2);
            this.LegacyPath = Environment.ExpandEnvironmentVariables(Resources.UserPrefsFilePath);
        }

        #region IUserPreferencesProvider implementation

        public event EventHandler<UserPreferences> PreferencesSaved;

        public UserPreferences LoadPreferences()
        {
            return this.LoadInternal();
        }

        public void SavePreferences(UserPreferences preferences)
        {
            this.SaveInternal(preferences);
        }

        #endregion

        #region System Events

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath != this.UserPrefsFilePath) return;

            var prefs = this.LoadInternal();
            this.PreferencesSaved?.Invoke(this, prefs);
        }

        #endregion

        #region Non-public methods

        private void SaveInternal(UserPreferences preferences)
        {
            try
            {
                var file = preferences.ToFile();
                this.SaveAsync(this.UserPrefsFilePath, file).GetAwaiter().GetResult();
                this.Logger.LogInformation("User preferences saved");
                this.SetupFileWatcher();
            }
            catch (Exception e)
            {
                this.Logger.LogException(e, "Failed to save user preferences");
            }
        }

        private UserPreferences LoadInternal()
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
                this.SetupFileWatcher();

                return UserPreferences.FromFile(file);
            }
            catch (Exception e)
            {
                this.Logger.LogException(e, "Failed to load user preferences");
                return UserPreferences.GetDefault();
            }
        }

        private void SetupFileWatcher()
        {
            if (this.FileSystemWatcher != null) return;

            var userPrefsDirectory = Path.GetDirectoryName(this.UserPrefsFilePath);
            if (!Directory.Exists(userPrefsDirectory)) return;

            this.FileSystemWatcher = new FileSystemWatcher(userPrefsDirectory);
            this.FileSystemWatcher.Changed += this.OnFileChanged;
            this.FileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

            this.Logger.LogInformation("Now watching for user prefs file changes");
        }

        private static readonly Dictionary<string, Action<UserPreferences, string>> MigrationActions = new()
        {
            { "ValheimGamePath", (p, v) => p.ValheimGamePath = v },
            { "ValheimServerPath", (p, v) => p.ValheimServerPath = v },
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
                        if (prefs.Servers.Count == 0)
                        {
                            prefs.Servers.Add(new ServerPreferences());
                        }

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
