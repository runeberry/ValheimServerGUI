using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ValheimServerGUI.Game
{
    public static class ValheimFileProviderExtensions
    {
        /// <summary>
        /// These are automatic backup files created by Valheim with the transition to
        /// the worlds_local folder on 6/20/22. Do not list these as world names.
        /// </summary>
        private static readonly Regex AutoBackupRegex = new(@"^.*?_backup_\d+?-\d+?");

        public static List<string> GetWorldNames(this IValheimFileProvider files)
        {
            try
            {
                var allNames = new List<string>();

                foreach (var info in files.WorldsFolders)
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

        public static bool IsWorldNameAvailable(this IValheimFileProvider files, string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return false;

            try
            {
                var paths = files.WorldsFolders.Select(p => Path.Join(p.FullName, $"{worldName}.fwl"));
                return !paths.Any(p => File.Exists(p));
            }
            catch
            {
                // Assume the world name is available if we cannot load the worlds folders
                return true;
            }
        }
    }
}
