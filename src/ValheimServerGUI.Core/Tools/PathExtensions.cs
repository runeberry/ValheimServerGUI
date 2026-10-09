using System;
using System.IO;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.Tools
{
    public static class PathExtensions
    {
        public static FileInfo GetFileInfo(string path, string? extension = null)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Strings.Path_FileNotDefined);
            }

            if (extension != null && !Path.HasExtension(path))
            {
                throw new ArgumentException(string.Format(Strings.Path_FileWrongType, extension, path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(string.Format(Strings.Path_FileNotFound, path));
            }

            return new FileInfo(path);
        }

        public static DirectoryInfo GetDirectoryInfo(string path, bool checkExists = false)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(Strings.Path_DirectoryNotDefined);
            }

            if (checkExists && !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(string.Format(Strings.Path_DirectoryNotFound, path));
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
