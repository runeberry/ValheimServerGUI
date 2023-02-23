using System;
using System.IO;

namespace ValheimServerGUI.Tools
{
    public static class PathExtensions
    {
        private static readonly string NL = Environment.NewLine;

        public static FileInfo GetFileInfo(string path, string extension = null)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"Cannot open file, path is not defined.");
            }

            if (extension != null && !Path.HasExtension(path))
            {
                throw new ArgumentException($"Cannot open file, must point to a valid {extension} file:{NL}{path}");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found at path:{NL}{path}");
            }

            return new FileInfo(path);
        }

        public static DirectoryInfo GetDirectoryInfo(string path, bool checkExists = false)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"Cannot open directory, path is not defined.");
            }

            if (checkExists && !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found at path:{NL}{path}");
            }

            return new DirectoryInfo(path);
        }
    }
}
