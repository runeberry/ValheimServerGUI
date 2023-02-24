using DeviceId;
using Semver;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ValheimServerGUI.Tools
{
    public static class AssemblyHelper
    {
        private static string _appVersion;
        private static string AppVersion => _appVersion ??= GetInformationalVersion();
        private const string BuildPrefix = "+build";
        private static string ClientCorrelationId;

        public static string GetApplicationVersion()
        {
            return AppVersion[..AppVersion.IndexOf(BuildPrefix)];
        }


        /// <remarks>
        /// Adapted from: https://rmauro.dev/add-build-time-to-your-csharp-assembly/
        /// Set as SourceRevisionId in csproj.
        /// </remarks>
        public static DateTime GetApplicationBuildDate()
        {
            var index = AppVersion.IndexOf(BuildPrefix) + BuildPrefix.Length;
            return DateTime.Parse(AppVersion[index..], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns...
        ///   * 1 if the provided version is newer than...
        ///   * -1 if the provided version is older than...
        ///   * 0 if the provided version is the same as...
        /// ...the current application version.
        /// Returns -2 if either version could not be parsed.
        /// </summary>
        /// <param name="otherVersion"></param>
        /// <returns></returns>
        public static int CompareVersion(string version)
        {
            try
            {
                var appVersion = SemVersion.Parse(GetApplicationVersion(), SemVersionStyles.Any);
                var otherVersion = SemVersion.Parse(version, SemVersionStyles.Any);
                return SemVersion.CompareSortOrder(otherVersion, appVersion);
            }
            catch
            {
                return -2;
            }
        }

        public static Version GetDotnetRuntimeVersion()
        {
            return Environment.Version;
        }

        public static string GetClientCorrelationId()
        {
            if (ClientCorrelationId != null) return ClientCorrelationId;

            var deviceId = new DeviceIdBuilder()
                .AddMacAddress()
                .AddMachineName()
                .ToString()
                .ToLowerInvariant();

            using var hash = MD5.Create();
            var hexStrings = hash.ComputeHash(Encoding.UTF8.GetBytes(deviceId)).Select(b => b.ToString("x2"));
            ClientCorrelationId = string.Join(string.Empty, hexStrings);

            return ClientCorrelationId;
        }

        public static CrashReport BuildCrashReport()
        {
            return new CrashReport
            {
                CrashReportId = Guid.NewGuid().ToString(),
                ClientCorrelationId = GetClientCorrelationId(),
                Timestamp = DateTime.UtcNow,
                AppVersion = GetApplicationVersion(),
                OsVersion = Environment.OSVersion.VersionString,
                DotnetVersion = Environment.Version.ToString(),
                CurrentCulture = CultureInfo.CurrentCulture?.ToString(),
                CurrentUICulture = CultureInfo.CurrentUICulture?.ToString(),
            };
        }

        #region Helper methods

        private static string GetInformationalVersion()
        {
            var attribute = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .FirstOrDefault() as AssemblyInformationalVersionAttribute;

            return attribute?.InformationalVersion ?? "0.0.0";
        }

        #endregion
    }
}
