using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Game
{
    public class ServerPreferences
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public string WorldName { get; set; }

        public bool Public { get; set; }

        public int Port { get; set; } = int.Parse(Resources.DefaultServerPort);

        public bool Crossplay { get; set; }

        public int SaveInterval { get; set; } = int.Parse(Resources.DefaultSaveInterval);

        public int BackupCount { get; set; } = int.Parse(Resources.DefaultBackupCount);

        public int BackupIntervalShort { get; set; } = int.Parse(Resources.DefaultBackupIntervalShort);

        public int BackupIntervalLong { get; set; } = int.Parse(Resources.DefaultBackupIntervalLong);

        public bool StartWithWindows { get; set; }

        public static ServerPreferences FromFile(ServerPreferencesFile file)
        {
            var prefs = new ServerPreferences();

            if (file == null) return prefs;

            prefs.Name = file.Name ?? prefs.Name;
            prefs.Password = file.Password ?? prefs.Password;
            prefs.WorldName = file.WorldName ?? prefs.WorldName;
            prefs.Public = file.Community ?? prefs.Public;
            prefs.Port = file.Port ?? prefs.Port;
            prefs.Crossplay = file.Crossplay ?? prefs.Crossplay;
            prefs.SaveInterval = file.SaveInterval ?? prefs.SaveInterval;
            prefs.BackupCount = file.BackupCount ?? prefs.BackupCount;
            prefs.BackupIntervalShort = file.BackupIntervalShort ?? prefs.BackupIntervalShort;
            prefs.BackupIntervalLong = file.BackupIntervalLong ?? prefs.BackupIntervalLong;
            prefs.StartWithWindows = file.StartWithWindows ?? prefs.StartWithWindows;

            return prefs;
        }

        public ServerPreferencesFile ToFile()
        {
            var file = new ServerPreferencesFile
            {
                Name = this.Name,
                Password = this.Password,
                WorldName = this.WorldName,
                Community = this.Public,
                Port = this.Port,
                Crossplay = this.Crossplay,
                SaveInterval = this.SaveInterval,
                BackupCount = this.BackupCount,
                BackupIntervalShort = this.BackupIntervalShort,
                BackupIntervalLong = this.BackupIntervalLong,
                StartWithWindows = this.StartWithWindows,
            };

            return file;
        }
    }
}
