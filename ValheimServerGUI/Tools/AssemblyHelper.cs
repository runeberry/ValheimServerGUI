using System;
using System.Linq;
using System.Reflection;

namespace ValheimServerGUI.Tools
{
    public static class AssemblyHelper
    {
        public static string GetApplicationVersion()
        {
            var attribute = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .FirstOrDefault() as AssemblyInformationalVersionAttribute;

            return attribute?.InformationalVersion ?? "0.0.0";
        }

        public static bool IsNewerVersion(string otherVersion)
        {
            try
            {
                var appVersion = new Version(GetApplicationVersion());
                otherVersion = otherVersion.Replace("v", ""); // Get rid of the leading "v" if it exists
                var v2 = new Version(otherVersion);
                return v2 > appVersion;
            }
            catch
            {
                return false;
            }
        }
    }
}
