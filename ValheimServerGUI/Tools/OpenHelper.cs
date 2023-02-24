using System;
using System.Diagnostics;

namespace ValheimServerGUI.Tools
{
    public static class OpenHelper
    {
        public static void OpenDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            path = Environment.ExpandEnvironmentVariables(path);

            try
            {
                // If this is a path to a valid file, open that file's directory
                path = PathExtensions.GetFileInfo(path).Directory.FullName;
            }
            catch
            {
                try
                {
                    // Otherwise, check if this is a path to a valid directory
                    path = PathExtensions.GetDirectoryInfo(path).FullName;
                }
                catch
                {
                    // If neither, do nothing
                    return;
                }
            }

            Process.Start("explorer.exe", path);
        }

        public static void OpenWebAddress(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            Process.Start("explorer.exe", url);
        }
    }
}
