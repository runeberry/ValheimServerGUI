using System;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Game
{
    public class ServerPreferences
    {
        public string ProfileName { get; set; } = Resources.DefaultServerProfileName;

        public DateTime LastSaved { get; set; } = DateTime.UnixEpoch;

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

        public bool AutoStart { get; set; }

        public string AdditionalArgs { get; set; }

        public string ServerExePath { get; set; }

        public string SaveDataFolderPath { get; set; }

        public bool WriteServerLogsToFile { get; set; } = true;

        public static ServerPreferences FromFile(ServerPreferencesFile file)
        {
            var prefs = new ServerPreferences();

            if (file == null) return prefs;

            prefs.ProfileName = !string.IsNullOrWhiteSpace(file.ProfileName) ? file.ProfileName : prefs.ProfileName;
            prefs.LastSaved = file.LastSaved ?? prefs.LastSaved;
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
            prefs.AutoStart = file.AutoStart ?? prefs.AutoStart;
            prefs.AdditionalArgs = file.AdditionalArgs ?? prefs.AdditionalArgs;
            prefs.ServerExePath = file.ServerExePath ?? prefs.ServerExePath;
            prefs.SaveDataFolderPath = file.SaveDataFolderPath ?? prefs.SaveDataFolderPath;
            prefs.WriteServerLogsToFile = file.WriteServerLogsToFile ?? prefs.WriteServerLogsToFile;

            return prefs;
        }

        public ServerPreferencesFile ToFile()
        {
            var file = new ServerPreferencesFile
            {
                ProfileName = ProfileName,
                LastSaved = LastSaved,
                Name = Name,
                Password = Password,
                WorldName = WorldName,
                Community = Public,
                Port = Port,
                Crossplay = Crossplay,
                SaveInterval = SaveInterval,
                BackupCount = BackupCount,
                BackupIntervalShort = BackupIntervalShort,
                BackupIntervalLong = BackupIntervalLong,
                AutoStart = AutoStart,
                AdditionalArgs = AdditionalArgs,
                ServerExePath = ServerExePath,
                SaveDataFolderPath = SaveDataFolderPath,
                WriteServerLogsToFile = WriteServerLogsToFile,
            };

            return file;
        }
    }
}
