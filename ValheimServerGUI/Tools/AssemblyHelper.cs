using DeviceId;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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

        public static Version GetDotnetRuntimeVersion()
        {
            return Environment.Version;
        }

        private static string ClientCorrelationId;

        public static string GetClientCorrelationId()
        {
            if (ClientCorrelationId != null) return ClientCorrelationId;

            var deviceId = new DeviceIdBuilder()
                .AddMacAddress()
                .AddMotherboardSerialNumber()
                .ToString()
                .ToLowerInvariant();

            using var hash = MD5.Create();
            var hexStrings = hash.ComputeHash(Encoding.UTF8.GetBytes(deviceId)).Select(b => b.ToString("x2"));
            ClientCorrelationId = string.Join(string.Empty, hexStrings);

            return ClientCorrelationId;
        }
    }
}
