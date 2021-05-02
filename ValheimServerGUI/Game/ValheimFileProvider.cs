using System;
using System.IO;

namespace ValheimServerGUI.Game
{
    public interface IValheimFileProvider
    {
        FileInfo GameExe { get; }

        FileInfo ServerExe { get; }

        DirectoryInfo SaveDataFolder { get; }

        DirectoryInfo WorldsFolder { get; }
    }

    public class ValheimFileProvider : IValheimFileProvider
    {
        private static readonly string NL = Environment.NewLine;

        private readonly IUserPreferencesProvider UserPrefsProvider;

        public ValheimFileProvider(IUserPreferencesProvider userPrefsProvider)
        {
            this.UserPrefsProvider = userPrefsProvider;
        }

        public FileInfo GameExe => this.GetFileInfo("ValheimGamePath", this.Current().ValheimGamePath, ".exe");

        public FileInfo ServerExe => this.GetFileInfo("ValheimServerPath", this.Current().ValheimServerPath, ".exe");

        public DirectoryInfo SaveDataFolder => this.GetDirectoryInfo("ValheimSaveDataFolder", this.Current().ValheimSaveDataFolder);

        public DirectoryInfo WorldsFolder => this.GetDirectoryInfo("ValheimWorldsFolder", Path.Join(this.Current().ValheimSaveDataFolder, "worlds"));

        #region Non-public methods

        private UserPreferences Current()
        {
            return this.UserPrefsProvider.LoadPreferences();
        }

        private FileInfo GetFileInfo(string prefKey, string path, string extension = null)
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

        private DirectoryInfo GetDirectoryInfo(string prefKey, string path)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"[{prefKey}] Cannot open directory, path is not defined.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"[{prefKey}] Directory not found at path:{NL}{path}");
            }

            return new DirectoryInfo(path);
        }

        #endregion
    }
}
