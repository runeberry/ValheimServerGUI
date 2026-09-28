using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Game
{
    public class UserPreferences
    {
        public static UserPreferences GetDefault() => new();

        // (jb, 2/19/23) This field was recorded, but never used.
        //public string ValheimGamePath { get; set; } = Resources.DefaultGamePath;

        public string ServerExePath { get; set; } = Resources.DefaultServerPath;
        public string SaveDataFolderPath { get; set; } = Resources.DefaultValheimSaveFolder;

        public bool CheckForUpdates { get; set; } = true;

        public bool StartWithWindows { get; set; }

        public bool StartMinimized { get; set; }

        public bool SaveProfileOnStart { get; set; } = true;

        public bool WriteApplicationLogsToFile { get; set; } = true;

        public bool EnablePasswordValidation { get; set; } = true;

        public bool DiscordStatusNotifications { get; set; }

        public string DiscordWebhookUrl { get; set; }

        // Per-event Discord notification preferences.
        // Default to true so existing users keep the same behavior after upgrading.
        public bool DiscordNotifyServerOnline { get; set; } = true;
        public bool DiscordNotifyServerOffline { get; set; } = true;
        public bool DiscordNotifyPlayerJoined { get; set; } = true;
        public bool DiscordNotifyPlayerLeft { get; set; } = true;
        public bool DiscordNotifyPlayerDied { get; set; } = true;
        public bool DiscordNotifyJoinCode { get; set; } = true;

        public List<ServerPreferences> Servers { get; set; } = new();

        public List<WorldPreferences> Worlds { get; set; } = new();

        public static UserPreferences FromFile(UserPreferencesFile file)
        {
            var prefs = new UserPreferences();

            if (file == null) return prefs;
            prefs.ServerExePath = file.ServerExePath ?? prefs.ServerExePath;
            prefs.SaveDataFolderPath = file.SaveDataFolderPath ?? prefs.SaveDataFolderPath;
            prefs.CheckForUpdates = file.CheckForUpdates ?? prefs.CheckForUpdates;
            prefs.StartWithWindows = file.StartWithWindows ?? prefs.StartWithWindows;
            prefs.StartMinimized = file.StartMinimized ?? prefs.StartMinimized;
            prefs.SaveProfileOnStart = file.SaveProfileOnStart ?? prefs.SaveProfileOnStart;
            prefs.WriteApplicationLogsToFile = file.WriteApplicationLogsToFile ?? prefs.WriteApplicationLogsToFile;
            prefs.EnablePasswordValidation = file.EnablePasswordValidation ?? prefs.EnablePasswordValidation;
            prefs.DiscordStatusNotifications = file.DiscordStatusNotifications ?? prefs.DiscordStatusNotifications;
            prefs.DiscordWebhookUrl = file.DiscordWebhookUrl ?? prefs.DiscordWebhookUrl;

            prefs.DiscordNotifyServerOnline =
                file.DiscordNotifyServerOnline ?? prefs.DiscordNotifyServerOnline;
            prefs.DiscordNotifyServerOffline =
                file.DiscordNotifyServerOffline ?? prefs.DiscordNotifyServerOffline;
            prefs.DiscordNotifyPlayerJoined =
                file.DiscordNotifyPlayerJoined ?? prefs.DiscordNotifyPlayerJoined;
            prefs.DiscordNotifyPlayerLeft =
                file.DiscordNotifyPlayerLeft ?? prefs.DiscordNotifyPlayerLeft;
            prefs.DiscordNotifyPlayerDied =
                file.DiscordNotifyPlayerDied ?? prefs.DiscordNotifyPlayerDied;
            prefs.DiscordNotifyJoinCode =
                file.DiscordNotifyJoinCode ?? prefs.DiscordNotifyJoinCode;

            if (file.Servers != null)
            {
                prefs.Servers = file.Servers
                    .Where(f => f != null)
                    .Select(f => ServerPreferences.FromFile(f))
                    .DistinctBy(f => f.ProfileName)
                    .ToList();
            }
            if (file.Worlds != null)
            {
                prefs.Worlds = file.Worlds
                    .Where(f => f != null)
                    .Select(f => WorldPreferences.FromFile(f))
                    .DistinctBy(f => f.WorldName)
                    .ToList();
            }

            return prefs;
        }

        public UserPreferencesFile ToFile()
        {
            var file = new UserPreferencesFile
            {
                ServerExePath = ServerExePath,
                SaveDataFolderPath = SaveDataFolderPath,
                CheckForUpdates = CheckForUpdates,
                StartWithWindows = StartWithWindows,
                StartMinimized = StartMinimized,
                SaveProfileOnStart = SaveProfileOnStart,
                WriteApplicationLogsToFile = WriteApplicationLogsToFile,
                EnablePasswordValidation = EnablePasswordValidation,
                DiscordStatusNotifications = DiscordStatusNotifications,
                DiscordWebhookUrl = DiscordWebhookUrl,

                DiscordNotifyServerOnline = DiscordNotifyServerOnline,
                DiscordNotifyServerOffline = DiscordNotifyServerOffline,
                DiscordNotifyPlayerJoined = DiscordNotifyPlayerJoined,
                DiscordNotifyPlayerLeft = DiscordNotifyPlayerLeft,
                DiscordNotifyPlayerDied = DiscordNotifyPlayerDied,
                DiscordNotifyJoinCode = DiscordNotifyJoinCode,

                Servers = new(),
                Worlds = new(),
            };

            if (Servers != null)
            {
                var servers = Servers
                    .Select(p => p.ToFile())
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProfileName))
                    .DistinctBy(p => p.ProfileName);

                file.Servers.AddRange(servers);
            }

            if (Worlds != null)
            {
                var worlds = Worlds
                    .Select(p => p.ToFile())
                    .Where(p => !string.IsNullOrWhiteSpace(p.WorldName))
                    .DistinctBy(p => p.WorldName);

                file.Worlds.AddRange(worlds);
            }

            return file;
        }
    }
}
