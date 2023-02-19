using System;
using System.IO;

namespace ValheimServerGUI.Game
{
    public interface IValheimFileProvider
    {
        /// <summary>
        /// The location of the Valheim dedicated server executable.
        /// </summary>
        FileInfo ServerExe { get; }

        /// <summary>
        /// The location of the Valheim dedicated server save data.
        /// </summary>
        DirectoryInfo SaveDataFolder { get; }

        /// <summary>
        /// The possible locations of Valheim world files.
        /// These directories may or may not exist.
        /// </summary>
        DirectoryInfo[] WorldsFolders { get; }
    }

    public class ValheimFileProvider : IValheimFileProvider
    {
        private static readonly string NL = Environment.NewLine;

        private readonly IUserPreferencesProvider UserPrefsProvider;

        public ValheimFileProvider(IUserPreferencesProvider userPrefsProvider)
        {
            UserPrefsProvider = userPrefsProvider;
        }

        public FileInfo ServerExe => GetFileInfo("ValheimServerPath", Current().ValheimServerPath, ".exe");

        public DirectoryInfo SaveDataFolder => GetDirectoryInfo("ValheimSaveDataFolder", Current().ValheimSaveDataFolder);

        public DirectoryInfo[] WorldsFolders => new[] {
            GetDirectoryInfo("ValheimWorldsFolder", Path.Join(Current().ValheimSaveDataFolder, "worlds"), false),
            GetDirectoryInfo("ValheimWorldsFolder", Path.Join(Current().ValheimSaveDataFolder, "worlds_local"), false),
        };

        #region Non-public methods

        private UserPreferences Current()
        {
            return UserPrefsProvider.LoadPreferences();
        }

        private static FileInfo GetFileInfo(string prefKey, string path, string extension = null)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"[{prefKey}] Cannot open file, path is not defined.");
            }

            if (extension != null && !Path.HasExtension(path))
            {
                throw new ArgumentException($"[{prefKey}] Cannot open file, must point to a valid {extension} file:{NL}{path}");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"[{prefKey}] File not found at path:{NL}{path}");
            }

            return new FileInfo(path);
        }

        private static DirectoryInfo GetDirectoryInfo(string prefKey, string path, bool checkExists = true)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"[{prefKey}] Cannot open directory, path is not defined.");
            }

            if (checkExists && !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"[{prefKey}] Directory not found at path:{NL}{path}");
            }

            return new DirectoryInfo(path);
        }

        #endregion
    }
}
