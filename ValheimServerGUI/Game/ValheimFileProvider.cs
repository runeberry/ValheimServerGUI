using System;
using System.Collections.Generic;
using System.IO;
using ValheimServerGUI.Tools.Preferences;

namespace ValheimServerGUI.Game
{
    public class ValheimFileProvider : IValheimFileProvider
    {
        private static readonly string NL = Environment.NewLine;

        private readonly IUserPreferences UserPrefs;

        public ValheimFileProvider(IUserPreferences userPrefs)
        {
            this.UserPrefs = userPrefs;
        }

        public FileInfo GameExe => this.GetFileInfo(PrefKeys.ValheimGamePath, ".exe");

        public FileInfo ServerExe => this.GetFileInfo(PrefKeys.ValheimServerPath, ".exe");

        public DirectoryInfo WorldsFolder => this.GetDirectoryInfo(PrefKeys.ValheimWorldsFolder);

        #region Non-public methods

        private FileInfo GetFileInfo(string prefKey, string extension = null)
        {
            var path = this.UserPrefs.GetEnvironmentValue(prefKey);

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

        private DirectoryInfo GetDirectoryInfo(string prefKey)
        {
            var path = this.UserPrefs.GetEnvironmentValue(prefKey);

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
