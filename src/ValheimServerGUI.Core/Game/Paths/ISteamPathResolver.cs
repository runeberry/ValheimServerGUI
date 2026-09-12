using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Locates the local desktop Steam client's install root, used to find in-game (Steam Cloud)
    /// world saves under <c>userdata/&lt;account&gt;/&lt;appid&gt;/remote</c>. Returns null (never throws)
    /// when Steam cannot be located, so callers degrade to "no cloud worlds".
    /// </summary>
    public interface ISteamPathResolver
    {
        /// <summary>The Steam install root, or null if it cannot be located.</summary>
        string? GetSteamInstallPath();
    }

    /// <summary>Reads the Steam install path from the Windows registry (HKCU\Software\Valve\Steam).</summary>
    [SupportedOSPlatform("windows")]
    public class WindowsSteamPathResolver : ISteamPathResolver
    {
        public string? GetSteamInstallPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                var value = key?.GetValue("SteamPath")?.ToString();
                return string.IsNullOrWhiteSpace(value) ? null : Environment.ExpandEnvironmentVariables(value);
            }
            catch
            {
                // No registry access / key absent: report Steam as not found.
                return null;
            }
        }
    }

    /// <summary>Probes the well-known Linux Steam install locations (native and Flatpak).</summary>
    public class LinuxSteamPathResolver : ISteamPathResolver
    {
        private readonly string _home;

        public LinuxSteamPathResolver()
            : this(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
        {
        }

        /// <summary>Test seam: inject the OS boundary (home directory).</summary>
        internal LinuxSteamPathResolver(string home)
        {
            _home = home;
        }

        public string? GetSteamInstallPath()
        {
            var candidates = new[]
            {
                Path.Join(_home, ".steam", "steam"),
                Path.Join(_home, ".local", "share", "Steam"),
                Path.Join(_home, ".steam", "root"),
                // Flatpak Steam
                Path.Join(_home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }
    }
}
