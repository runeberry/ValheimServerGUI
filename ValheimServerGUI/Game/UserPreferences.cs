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

        public bool StartWithWindows { get; set; }

        public bool StartServerAutomatically { get; set; }

        public bool StartMinimized { get; set; }

        public bool CheckServerRunning { get; set; } = true;

        public bool CheckForUpdates { get; set; } = true;

        public List<ServerPreferences> Servers { get; set; } = new();



        public static UserPreferences FromFile(UserPreferencesFile file)
        {
            var prefs = new UserPreferences();

            if (file == null) return prefs;

            prefs.ValheimGamePath = file.ValheimGamePath ?? prefs.ValheimGamePath;
            prefs.ValheimServerPath = file.ValheimServerPath ?? prefs.ValheimServerPath;
            prefs.ValheimSaveDataFolder = file.ValheimSaveDataFolder ?? prefs.ValheimSaveDataFolder;
            prefs.StartWithWindows = file.StartWithWindows ?? prefs.StartWithWindows;
            prefs.StartServerAutomatically = file.StartServerAutomatically ?? prefs.StartServerAutomatically;
            prefs.StartMinimized = file.StartMinimized ?? prefs.StartMinimized;
            prefs.CheckServerRunning = file.CheckServerRunning ?? prefs.CheckServerRunning;
            prefs.CheckForUpdates = file.CheckForUpdates ?? prefs.CheckForUpdates;

            if (file.Servers != null)
            {
                prefs.Servers = file.Servers.Select(f => ServerPreferences.FromFile(f)).ToList();
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
                StartWithWindows = this.StartWithWindows,
                StartServerAutomatically = this.StartServerAutomatically,
                StartMinimized = this.StartMinimized,
                CheckServerRunning = this.CheckServerRunning,
                CheckForUpdates = this.CheckForUpdates,
                Servers = new(),
            };

            if (this.Servers != null)
            {
                var servers = this.Servers
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name)) // Remove servers with no name
                    .DistinctBy(p => p.Name) // Remove duplicate entries by server name
                    .Select(p => p.ToFile());

                file.Servers.AddRange(servers);
            }

            return file;
        }
    }
}
