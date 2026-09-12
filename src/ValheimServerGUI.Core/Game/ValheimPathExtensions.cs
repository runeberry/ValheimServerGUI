using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Game
{
    public static class ValheimPathExtensions
    {
        /// <summary>
        /// These are automatic backup files created by Valheim with the transition to
        /// the worlds_local folder on 6/20/22. Do not list these as world names.
        /// Covers both the "_backup_&lt;date&gt;-&lt;time&gt;" and "_backup_auto-&lt;timestamp&gt;" naming schemes.
        /// </summary>
        private static readonly Regex AutoBackupRegex = new(@"^.*?_backup_(auto-\d|\d+?-\d+?)");

        public static FileInfo GetValidatedServerExe(this IValheimServerOptions options)
        {
            // No extension constraint: the dedicated-server binary is valheim_server.exe on Windows
            // but valheim_server.x86_64 on Linux, so validation is simply "an existing file".
            // A null path coalesces to empty so GetFileInfo raises the clean "path not defined" error.
            return PathExtensions.GetFileInfo(options.ServerExePath ?? string.Empty);
        }

        public static DirectoryInfo GetValidatedSaveDataFolder(this IValheimServerOptions options)
        {
            return PathExtensions.GetDirectoryInfo(options.SaveDataFolderPath ?? string.Empty, true);
        }

        public static List<string> GetWorldNames(this DirectoryInfo saveDataFolder)
        {
            try
            {
                return saveDataFolder.GetWorldsFolders()
                    .Where(info => Directory.Exists(info.FullName))
                    .SelectMany(GetWorldNamesInFolder)
                    // §15 #6: a world present in both "worlds" and "worlds_local" is one world, not two.
                    .Distinct(System.StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                // Return an empty list of names if we cannot load the worlds folders
                return new();
            }
        }

        public static bool IsWorldNameAvailable(this DirectoryInfo saveDataFolder, string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return false;

            // §15 #4: availability derives from the SAME backup-excluded enumeration used to list
            // worlds, so the two can never disagree -- a backup file is not a world and must not make
            // the name unavailable. Comparison is case-insensitive (one world across both OSes, E20).
            return !saveDataFolder.GetWorldNames().Contains(worldName, System.StringComparer.OrdinalIgnoreCase);
        }

        #region Helper methods

        private static IEnumerable<DirectoryInfo> GetWorldsFolders(this DirectoryInfo saveDataFolder)
        {
            yield return PathExtensions.GetDirectoryInfo(Path.Join(saveDataFolder.FullName, "worlds"));
            yield return PathExtensions.GetDirectoryInfo(Path.Join(saveDataFolder.FullName, "worlds_local"));
        }

        /// <summary>
        /// The single source of truth for which world names exist in a "worlds"/"worlds_local" folder,
        /// in both the legacy ("&lt;World&gt;.fwl" file) and Valheim 1.0+ ("&lt;World&gt;/*.fwl2" folder)
        /// formats, with backups excluded. Both listing and availability route through here.
        /// </summary>
        private static IEnumerable<string> GetWorldNamesInFolder(DirectoryInfo worldsFolder)
        {
            // Legacy format: one "<WorldName>.fwl" file per world
            foreach (var file in worldsFolder.GetFiles("*.fwl").Where(f => !AutoBackupRegex.IsMatch(f.Name)))
            {
                yield return Path.GetFileNameWithoutExtension(file.Name);
            }

            // Valheim 1.0+ format: a "<WorldName>" folder containing a "*.fwl2" file (non-recursive)
            foreach (var dir in worldsFolder.GetDirectories().Where(d => !AutoBackupRegex.IsMatch(d.Name) && d.GetFiles("*.fwl2").Any()))
            {
                yield return dir.Name;
            }
        }

        #endregion
    }
}
