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
        /// </summary>
        private static readonly Regex AutoBackupRegex = new(@"^.*?_backup_\d+?-\d+?");

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

                    allNames.AddRange(info
                        .GetFiles("*.fwl")
                        .Where(f => !AutoBackupRegex.IsMatch(f.Name))
                        .Select(f => Path.GetFileNameWithoutExtension(f.FullName)));
                }

                return allNames;
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
                return !saveDataFolder.GetWorldsFolders()
                    .Select(p => Path.Join(p.FullName, $"{worldName}.fwl"))
                    .Any(p => File.Exists(p));
            }
            catch
            {
                // Assume the world name is available if we cannot load the worlds folders
                return true;
            }
        }

        #region Helper methods

        private static IEnumerable<DirectoryInfo> GetWorldsFolders(this DirectoryInfo saveDataFolder)
        {
            yield return PathExtensions.GetDirectoryInfo(Path.Join(saveDataFolder.FullName, "worlds"));
            yield return PathExtensions.GetDirectoryInfo(Path.Join(saveDataFolder.FullName, "worlds_local"));
        }

        #endregion
    }
}
