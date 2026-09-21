using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public class ServerPreferences
    {
        public string ProfileName { get; set; } = CoreConstants.DefaultServerProfileName;

        public DateTime LastSaved { get; set; } = DateTime.UnixEpoch;

        public string? Name { get; set; }

        public string? Password { get; set; }

        public string? WorldName { get; set; }

        public bool Public { get; set; }

        public int Port { get; set; } = CoreConstants.DefaultServerPort;

        public bool Crossplay { get; set; }

        public int SaveInterval { get; set; } = CoreConstants.DefaultSaveInterval;

        public int BackupCount { get; set; } = CoreConstants.DefaultBackupCount;

        public int BackupIntervalShort { get; set; } = CoreConstants.DefaultBackupIntervalShort;

        public int BackupIntervalLong { get; set; } = CoreConstants.DefaultBackupIntervalLong;

        public bool AutoStart { get; set; }

        public string? AdditionalArgs { get; set; }

        public string? ServerExePath { get; set; }

        public string? SaveDataFolderPath { get; set; }

        public bool WriteServerLogsToFile { get; set; } = true;

        /// <summary>
        /// When true, the server runs in permitted-list mode: only players on <c>permittedlist.txt</c> may
        /// join and the ban list is ignored (see <see cref="PlayerAccessListRules"/>). When false, anyone
        /// joins unless banned. The three gating files are generated from this flag + <see cref="PlayerRoles"/>
        /// at server start.
        /// </summary>
        public bool UsePermittedList { get; set; }

        /// <summary>
        /// The profile's per-player roles, keyed by <see cref="PlayerInfo.Key"/> (<c>"{Platform}:{PlayerId}"</c>).
        /// A player absent from the map has no role. This is VSG's source of truth for gating; the three
        /// <c>*.txt</c> files are regenerated from it at server start.
        /// </summary>
        public Dictionary<string, PlayerRoleEntry> PlayerRoles { get; set; } = new();

        // The lowercase string tokens persisted for each role in the JSON file (stable across enum reorders).
        private const string RoleAdmin = "admin";
        private const string RolePermitted = "permitted";
        private const string RoleBanned = "banned";

        public static ServerPreferences FromFile(ServerPreferencesFile? file)
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
            prefs.UsePermittedList = file.UsePermittedList ?? prefs.UsePermittedList;

            if (file.PlayerRoles != null)
            {
                foreach (var (key, entry) in file.PlayerRoles)
                {
                    if (string.IsNullOrWhiteSpace(key) || entry == null) continue;
                    if (TryParseRole(entry.Role, out var role))
                        prefs.PlayerRoles[key] = new PlayerRoleEntry(role, entry.PlatformRaw);
                }
            }

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
                UsePermittedList = UsePermittedList,
            };

            // Written only when there are roles, so profiles that never used the feature stay byte-identical.
            if (PlayerRoles.Count > 0)
            {
                file.PlayerRoles = PlayerRoles.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new PlayerRoleFileEntry
                    {
                        Role = RoleToString(kvp.Value.Role),
                        PlatformRaw = kvp.Value.PlatformRaw,
                    });
            }

            return file;
        }

        private static string RoleToString(PlayerRole role) => role switch
        {
            PlayerRole.Admin => RoleAdmin,
            PlayerRole.Permitted => RolePermitted,
            PlayerRole.Banned => RoleBanned,
            _ => RoleBanned,
        };

        private static bool TryParseRole(string? value, out PlayerRole role)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case RoleAdmin: role = PlayerRole.Admin; return true;
                case RolePermitted: role = PlayerRole.Permitted; return true;
                case RoleBanned: role = PlayerRole.Banned; return true;
                default: role = default; return false;
            }
        }
    }
}
