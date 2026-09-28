using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public class UserPreferences
    {
        public static UserPreferences GetDefault() => new();

        // Default paths are OS-specific and no longer hard-coded here: UserPreferencesProvider fills
        // these from IValheimPathResolver when a loaded value is blank (see ApplyPathDefaults).
        public string ServerExePath { get; set; } = string.Empty;

        public string SaveDataFolderPath { get; set; } = string.Empty;

        public bool CheckForUpdates { get; set; } = true;

        public bool StartWithWindows { get; set; }

        public bool StartMinimized { get; set; }

        public bool SaveProfileOnStart { get; set; } = true;

        public bool WriteApplicationLogsToFile { get; set; } = true;

        public bool EnablePasswordValidation { get; set; } = true;

        /// <summary>
        /// Profile name last loaded/saved in a window. Preferred by the startup profile selection (§2.3
        /// / §16.2) over the most-recently-saved fallback, so relaunch reopens where the user left off.
        /// </summary>
        public string? LastActiveProfile { get; set; }

        /// <summary>UI theme (§16.2 enhancement). Defaults to following the OS.</summary>
        public AppTheme Theme { get; set; } = AppTheme.System;

        public List<ServerPreferences> Servers { get; set; } = new();

        public List<WorldPreferences> Worlds { get; set; } = new();

        /// <summary>
        /// App-global player defaults (the Manage Players lists), keyed by <see cref="PlayerInfo.Key"/>. Each
        /// entry's default role applies on every server unless the profile overrides it
        /// (see <see cref="PlayerRoleResolver"/>).
        /// </summary>
        public Dictionary<string, PlayerDefaultEntry> PlayerDefaults { get; set; } = new();

        // The lowercase string tokens persisted for each category (stable across enum reorders).
        private const string CategoryMyAccount = "myaccount";
        private const string CategoryFriend = "friend";
        private const string CategoryBanned = "banned";

        public static UserPreferences FromFile(UserPreferencesFile? file)
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
            prefs.LastActiveProfile = file.LastActiveProfile ?? prefs.LastActiveProfile;
            prefs.Theme = file.Theme ?? prefs.Theme;

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

            if (file.PlayerDefaults != null)
            {
                foreach (var (key, entry) in file.PlayerDefaults)
                {
                    // Unknown tokens (e.g. from a newer version) drop the entry rather than guess.
                    if (string.IsNullOrWhiteSpace(key) || entry == null) continue;
                    if (!TryParseCategory(entry.Category, out var category)) continue;
                    if (!PlayerRoleTokens.TryParse(entry.DefaultRole, out var role)) role = PlayerRole.None;
                    prefs.PlayerDefaults[key] = new PlayerDefaultEntry(category, role, entry.PlatformRaw).Normalized();
                }
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
                LastActiveProfile = LastActiveProfile,
                Theme = Theme,
                Servers = new(),
                Worlds = new(),
            };

            if (Servers != null)
            {
                var servers = Servers
                    .Select(p => p.ToFile())
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProfileName)) // Remove profiles with no name
                    .DistinctBy(p => p.ProfileName); // Remove duplicate entries by profile name

                file.Servers.AddRange(servers);
            }

            if (Worlds != null)
            {
                var worlds = Worlds
                    .Select(p => p.ToFile())
                    .Where(p => !string.IsNullOrWhiteSpace(p.WorldName)) // Remove world settings with no name
                    .DistinctBy(p => p.WorldName); // Remove duplicate entries by world name

                file.Worlds.AddRange(worlds);
            }

            // Written only when there are defaults, so files that never used the feature stay byte-identical.
            if (PlayerDefaults is { Count: > 0 })
            {
                file.PlayerDefaults = PlayerDefaults.ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        var entry = kvp.Value.Normalized();
                        return new PlayerDefaultFileEntry
                        {
                            Category = CategoryToString(entry.Category),
                            DefaultRole = PlayerRoleTokens.ToToken(entry.DefaultRole),
                            PlatformRaw = entry.PlatformRaw,
                        };
                    });
            }

            return file;
        }

        private static string CategoryToString(PlayerCategory category) => category switch
        {
            PlayerCategory.MyAccount => CategoryMyAccount,
            PlayerCategory.Friend => CategoryFriend,
            _ => CategoryBanned,
        };

        private static bool TryParseCategory(string? value, out PlayerCategory category)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case CategoryMyAccount: category = PlayerCategory.MyAccount; return true;
                case CategoryFriend: category = PlayerCategory.Friend; return true;
                case CategoryBanned: category = PlayerCategory.Banned; return true;
                default: category = default; return false;
            }
        }
    }
}
