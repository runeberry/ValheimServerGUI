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

            return file;
        }
    }
}
