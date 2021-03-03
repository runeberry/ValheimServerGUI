using System.Diagnostics;
using System.Reflection;

namespace ValheimServerGUI.Tools
{
    public static class AssemblyHelper
    {
        public static string GetApplicationVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var info = FileVersionInfo.GetVersionInfo(assembly.Location);
            return info.ProductVersion;
        }
    }
}
