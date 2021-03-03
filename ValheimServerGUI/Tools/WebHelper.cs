using System.Diagnostics;

namespace ValheimServerGUI.Tools
{
    public static class WebHelper
    {
        public static void OpenWebAddress(string url)
        {
            Process.Start("explorer.exe", url);
        }
    }
}
