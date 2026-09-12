using System;
using System.IO;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game.Paths
{
    /// <summary>
    /// The Linux Steam-path resolver probes the well-known install locations in priority order and
    /// returns the first that exists, or null when Steam is absent (so cloud-world enumeration degrades
    /// cleanly). Exercised against a throwaway temp home so it never touches the real one.
    /// </summary>
    public class SteamPathResolverTests : IDisposable
    {
        private readonly string _home;

        public SteamPathResolverTests()
        {
            _home = Path.Join(Path.GetTempPath(), "vsg-steam-" + Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(_home);
        }

        public void Dispose()
        {
            if (Directory.Exists(_home)) Directory.Delete(_home, recursive: true);
        }

        [Fact]
        public void NoSteamInstall_ReturnsNull()
        {
            var resolver = new LinuxSteamPathResolver(_home);
            Assert.Null(resolver.GetSteamInstallPath());
        }

        [Fact]
        public void SecondaryLocationOnly_IsFound()
        {
            // Only the non-preferred ~/.local/share/Steam exists.
            var localShareSteam = Path.Join(_home, ".local", "share", "Steam");
            Directory.CreateDirectory(localShareSteam);

            var resolver = new LinuxSteamPathResolver(_home);
            Assert.Equal(localShareSteam, resolver.GetSteamInstallPath());
        }

        [Fact]
        public void PreferredLocation_WinsOverSecondary()
        {
            var preferred = Path.Join(_home, ".steam", "steam");
            Directory.CreateDirectory(preferred);
            Directory.CreateDirectory(Path.Join(_home, ".local", "share", "Steam"));

            var resolver = new LinuxSteamPathResolver(_home);
            Assert.Equal(preferred, resolver.GetSteamInstallPath());
        }
    }
}
