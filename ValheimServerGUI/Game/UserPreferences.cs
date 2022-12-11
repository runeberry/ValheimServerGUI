using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Game
{
    public class UserPreferences
    {
        public static UserPreferences GetDefault() => new();

        public string ValheimGamePath { get; set; } = Resources.DefaultGamePath;

        public string ValheimServerPath { get; set; } = Resources.DefaultServerPath;

        public string ValheimSaveDataFolder { get; set; } = Resources.DefaultValheimSaveFolder;

        public bool CheckForUpdates { get; set; } = true;

        public bool StartWithWindows { get; set; }

        public bool StartMinimized { get; set; }

        public bool SaveProfileOnStart { get; set; } = true;

        public List<ServerPreferences> Servers { get; set; } = new();

        public static UserPreferences FromFile(UserPreferencesFile file)
        {
            var prefs = new UserPreferences();

            if (file == null) return prefs;

            prefs.ValheimGamePath = file.ValheimGamePath ?? prefs.ValheimGamePath;
            prefs.ValheimServerPath = file.ValheimServerPath ?? prefs.ValheimServerPath;
            prefs.ValheimSaveDataFolder = file.ValheimSaveDataFolder ?? prefs.ValheimSaveDataFolder;
            prefs.CheckForUpdates = file.CheckForUpdates ?? prefs.CheckForUpdates;
            prefs.StartWithWindows = file.StartWithWindows ?? prefs.StartWithWindows;
            prefs.StartMinimized = file.StartMinimized ?? prefs.StartMinimized;
            prefs.SaveProfileOnStart = file.SaveProfileOnStart ?? prefs.SaveProfileOnStart;

            if (file.Servers != null)
            {
                prefs.Servers = file.Servers
                    .Where(f => f != null)
                    .Select(f => ServerPreferences.FromFile(f))
                    .DistinctBy(f => f.ProfileName)
                    .ToList();
            }

            return prefs;
        }

        public UserPreferencesFile ToFile()
        {
            var file = new UserPreferencesFile
            {
                ValheimGamePath = this.ValheimGamePath,
                ValheimServerPath = this.ValheimServerPath,
                ValheimSaveDataFolder = this.ValheimSaveDataFolder,
                CheckForUpdates = this.CheckForUpdates,
                StartWithWindows = this.StartWithWindows,
                StartMinimized = this.StartMinimized,
                SaveProfileOnStart = this.SaveProfileOnStart,
                Servers = new(),
            };

            if (this.Servers != null)
            {
                var servers = this.Servers
                    .Select(p => p.ToFile())
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProfileName)) // Remove profiles with no name
                    .DistinctBy(p => p.ProfileName); // Remove duplicate entries by profile name

                file.Servers.AddRange(servers);
            }

            return file;
        }
    }
}
