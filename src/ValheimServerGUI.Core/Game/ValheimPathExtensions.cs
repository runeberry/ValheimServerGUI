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

        /// <summary>
        /// The admin/ban/permit list files live directly in the save-data root (the <c>-savedir</c>),
        /// alongside <c>worlds</c>/<c>worlds_local</c>, shared by every world in that savedir -- exactly
        /// where <c>ZNet</c> reads them from (<c>Utils.GetSaveDataPath()/adminlist.txt</c> etc.). These
        /// mirror the <see cref="GetWorldsFolders"/> savedir-root pattern.
        /// </summary>
        public static FileInfo GetAdminListFile(this DirectoryInfo saveDataFolder)
            => GetSaveDataRootFile(saveDataFolder, "adminlist.txt");

        public static FileInfo GetBannedListFile(this DirectoryInfo saveDataFolder)
            => GetSaveDataRootFile(saveDataFolder, "bannedlist.txt");

        public static FileInfo GetPermittedListFile(this DirectoryInfo saveDataFolder)
            => GetSaveDataRootFile(saveDataFolder, "permittedlist.txt");

        private static FileInfo GetSaveDataRootFile(DirectoryInfo saveDataFolder, string fileName)
            => new(Path.Join(saveDataFolder.FullName, fileName));

        /// <summary>
        /// Moves a list file aside to the first free increment, preserving any manual entries before generation
        /// overwrites the original: <c>permittedlist.txt</c> → <c>permittedlist.bak.txt</c>, then
        /// <c>permittedlist.bak.2.txt</c>, <c>permittedlist.bak.3.txt</c>, …. Returns the destination it moved
        /// to, or <c>null</c> when the file does not exist or the move fails (an <see cref="IOException"/>, e.g.
        /// the path is locked or occupied), leaving the caller to decide how to react. Generic over any of the
        /// three list files.
        /// </summary>
        public static FileInfo? BackupListFile(FileInfo file)
        {
            if (!file.Exists) return null;

            var dir = file.DirectoryName ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(file.Name);
            var ext = Path.GetExtension(file.Name);

            for (var i = 1; ; i++)
            {
                // First increment is unnumbered (".bak"); subsequent ones carry the count (".bak.2", ".bak.3").
                var candidate = i == 1 ? $"{name}.bak{ext}" : $"{name}.bak.{i}{ext}";
                var dest = new FileInfo(Path.Combine(dir, candidate));
                if (dest.Exists) continue;

                try
                {
                    File.Move(file.FullName, dest.FullName);
                    return dest;
                }
                catch (IOException)
                {
                    return null;
                }
            }
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
