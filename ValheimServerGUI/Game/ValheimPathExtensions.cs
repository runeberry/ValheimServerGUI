using System;
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
        /// Valheim backup names. Supports both legacy flat-file backups and the
        /// Valheim 1.0 directory-based backup format, for example:
        ///   World_backup_20260906-115918.fwl
        ///   World_backup_auto-20260906114306.fwl
        ///   World_backup_auto-20260909-155858 (directory)
        /// Backups must never be offered as normal playable worlds in the selector.
        /// </summary>
        private static readonly Regex BackupNameRegex = new(
            @"_backup_(?:auto-)?\d{8}(?:-?\d{6})?(?:\.[^.]+)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static FileInfo GetValidatedServerExe(this IValheimServerOptions options)
        {
            return PathExtensions.GetFileInfo(options.ServerExePath, ".exe");
        }

        public static DirectoryInfo GetValidatedSaveDataFolder(this IValheimServerOptions options)
        {
            return PathExtensions.GetDirectoryInfo(options.SaveDataFolderPath, true);
        }

        public static List<string> GetWorldNames(this DirectoryInfo saveDataFolder)
        {
            try
            {
                var allNames = new List<string>();

                foreach (var info in saveDataFolder.GetWorldsFolders())
                {
                    if (!Directory.Exists(info.FullName)) continue;

                    // Pre-1.0 format: worlds_local\WorldName.fwl
                    allNames.AddRange(info
                        .GetFiles("*.fwl")
                        .Where(f => !BackupNameRegex.IsMatch(f.Name))
                        .Select(f => Path.GetFileNameWithoutExtension(f.FullName)));

                    // Valheim 1.0 format: worlds_local\WorldName\_main.<n>.fwl2
                    allNames.AddRange(info
                        .GetDirectories()
                        .Where(d => !BackupNameRegex.IsMatch(d.Name))
                        .Where(IsValheim10WorldDirectory)
                        .Select(d => d.Name));
                }

                return allNames
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
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

            try
            {
                foreach (var worldsFolder in saveDataFolder.GetWorldsFolders())
                {
                    // Pre-1.0 format
                    var legacyWorldFile = Path.Join(worldsFolder.FullName, $"{worldName}.fwl");
                    if (File.Exists(legacyWorldFile)) return false;

                    // Valheim 1.0 format. The directory name itself is the world name.
                    var valheim10WorldDirectory = Path.Join(worldsFolder.FullName, worldName);
                    if (Directory.Exists(valheim10WorldDirectory)) return false;
                }

                return true;
            }
            catch
            {
                // Assume the world name is available if we cannot load the worlds folders
                return true;
            }
        }

        #region Helper methods

        private static bool IsValheim10WorldDirectory(DirectoryInfo directory)
        {
            try
            {
                // New 1.0 saves use versioned _main metadata files such as _main.8.fwl2.
                return directory.GetFiles("_main.*.fwl2").Any();
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<DirectoryInfo> GetWorldsFolders(this DirectoryInfo saveDataFolder)
        {
            yield return PathExtensions.GetDirectoryInfo(Path.Join(saveDataFolder.FullName, "worlds"));
            yield return PathExtensions.GetDirectoryInfo(Path.Join(saveDataFolder.FullName, "worlds_local"));
        }

        #endregion
    }
}
