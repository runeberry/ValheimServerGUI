using DeviceId;
using Semver;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Tools
{
    public static class AssemblyHelper
    {
        private static string? _appVersion;
        private static string AppVersion => _appVersion ??= GetInformationalVersion();
        private const string BuildPrefix = "+build";
        private static string? ClientCorrelationId;

        public static string GetApplicationVersion()
        {
            // The informational version carries an optional "+build<timestamp>" suffix (set as
            // SourceRevisionId in the csproj). Guard its absence: without a build suffix (e.g. a
            // test host, or a build that did not stamp one) IndexOf returns -1 and the range would
            // throw. Fall back to the whole version string in that case.
            var index = AppVersion.IndexOf(BuildPrefix);
            return index < 0 ? AppVersion : AppVersion[..index];
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
            return CompareVersions(GetApplicationVersion(), version);
        }

        /// <summary>
        /// Pure comparison backing <see cref="CompareVersion(string)"/>. Returns...
        ///   * 1 if <paramref name="otherVersion"/> is newer than <paramref name="currentVersion"/>
        ///   * -1 if <paramref name="otherVersion"/> is older than <paramref name="currentVersion"/>
        ///   * 0 if the two are equal
        /// ...using semantic-version precedence, in which a stable release outranks its own
        /// pre-releases (e.g. 2.4.0 is newer than 2.4.0-rc.1). Returns -2 if either version could
        /// not be parsed. Both arguments accept an optional leading "v", as GitHub release tags carry.
        /// </summary>
        public static int CompareVersions(string currentVersion, string otherVersion)
        {
            try
            {
                var current = SemVersion.Parse(currentVersion, SemVersionStyles.Any);
                var other = SemVersion.Parse(otherVersion, SemVersionStyles.Any);
                return SemVersion.CompareSortOrder(other, current);
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
