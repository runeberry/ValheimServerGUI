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

        public static string GetValidFileName(string filename, bool addTimestamp = false)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return "file";
            }

            if (filename.Length > 150)
            {
                // Max filename length is likely closer to 255, but I'm just gonna play it safe
                filename = filename[..150];
            }

            if (addTimestamp)
            {
                filename = $"{filename}_{DateTime.Now.ToFilenameISOFormat()}";
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '-');
            }

            return filename;
        }
    }
}
