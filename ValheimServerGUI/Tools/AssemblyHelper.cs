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
    }
}
