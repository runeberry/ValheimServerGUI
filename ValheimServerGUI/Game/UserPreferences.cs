using System.Linq;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Game
{
    public class UserPreferences
    {
        private static readonly int DefaultServerPort = int.Parse(Resources.DefaultServerPort);

        public static UserPreferences GetDefault() => new();

        public string ValheimGamePath { get; set; } = Resources.DefaultGamePath;

        public string ValheimServerPath { get; set; } = Resources.DefaultServerPath;

        public string ValheimSaveDataFolder { get; set; } = Resources.DefaultValheimSaveFolder;

        public bool StartWithWindows { get; set; }

        public bool StartServerAutomatically { get; set; }

        public bool StartMinimized { get; set; }

        public bool CheckServerRunning { get; set; } = true;

        public bool CheckForUpdates { get; set; } = true;

        public string ServerName { get; set; }

        public string ServerPassword { get; set; }

        public string ServerWorldName { get; set; }

        public bool ServerPublic { get; set; }

        public int ServerPort { get; set; } = DefaultServerPort;

        public bool ServerCrossplay { get; set; }

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

            var server = file.Servers?.FirstOrDefault();

            if (server != null)
            {
                prefs.ServerName = server.Name ?? prefs.ServerName;
                prefs.ServerPassword = server.Password ?? prefs.ServerPassword;
                prefs.ServerWorldName = server.WorldName ?? prefs.ServerWorldName;
                prefs.ServerPublic = server.CommunityServer ?? prefs.ServerPublic;
                prefs.ServerPort = server.Port ?? prefs.ServerPort;
                prefs.ServerCrossplay = server.Crossplay ?? prefs.ServerCrossplay;
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
                Servers = new()
                {
                    new()
                    {
                        Name = this.ServerName,
                        Password = this.ServerPassword,
                        WorldName = this.ServerWorldName,
                        CommunityServer = this.ServerPublic,
                        Port = this.ServerPort,
                        Crossplay = this.ServerCrossplay,
                    }
                }
            };

            return file;
        }
    }
}
