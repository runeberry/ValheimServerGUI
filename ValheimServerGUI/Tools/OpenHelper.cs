using System;
using System.Diagnostics;

namespace ValheimServerGUI.Tools
{
    public static class OpenHelper
    {
        public static void OpenDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            Process.Start("explorer.exe", Environment.ExpandEnvironmentVariables(path));
        }

        public static void OpenWebAddress(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            Process.Start("explorer.exe", url);
        }
    }
}
