using System.IO;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game.Paths
{
    /// <summary>
    /// E53-adjacent: the per-OS path resolvers replace the WinForms <c>Properties.Resources</c> path
    /// members. The Linux resolver uses XDG / <c>$HOME</c> (fully exercised here since we run on Linux);
    /// the Windows resolver's paths are OS-skipped, but its binary name is asserted everywhere.
    /// </summary>
    public class ValheimPathResolverTests
    {
        private const string Home = "/home/tester";

        private static string Join(params string[] parts) => Path.Join(parts);

        [Fact]
        public void Linux_BinaryName_IsServerX8664()
        {
            var resolver = new LinuxValheimPathResolver(Home, xdgDataHome: null);
            Assert.Equal("valheim_server.x86_64", resolver.ServerBinaryName);
        }

        [Fact]
        public void Linux_AppDataPaths_DefaultToXdgLocalShare()
        {
            var resolver = new LinuxValheimPathResolver(Home, xdgDataHome: null);
            var root = Join(Home, ".local", "share", "ValheimServerGUI");

            Assert.Equal(Join(root, "userprefs.json"), resolver.UserPrefsFilePath);
            Assert.Equal(Join(root, "userprefs.txt"), resolver.LegacyUserPrefsFilePath);
            Assert.Equal(Join(root, "players-cache.json"), resolver.PlayerListFilePath);
            Assert.Equal(Join(root, "logs"), resolver.LogsFolderPath);
        }

        [Fact]
        public void Linux_AppDataPaths_HonorXdgDataHomeOverride()
        {
            var resolver = new LinuxValheimPathResolver(Home, xdgDataHome: "/custom/xdg");
            Assert.Equal(Join("/custom/xdg", "ValheimServerGUI", "userprefs.json"), resolver.UserPrefsFilePath);
        }

        [Fact]
        public void Linux_ServerAndSavePaths()
        {
            var resolver = new LinuxValheimPathResolver(Home, xdgDataHome: null);

            Assert.Equal(
                Join(Home, ".steam", "steam", "steamapps", "common", "Valheim dedicated server", "valheim_server.x86_64"),
                resolver.DefaultServerPath);
            Assert.Equal(Join(Home, ".config", "unity3d", "IronGate", "Valheim"), resolver.DefaultSaveDataFolder);
        }

        // derive-never-mirror: the named path members must be GetAppDataPath(<relative name>), not a
        // second copy of the layout kept in step.
        [Fact]
        public void NamedPaths_DeriveFromGetAppDataPath()
        {
            var resolver = new LinuxValheimPathResolver(Home, xdgDataHome: null);

            Assert.Equal(resolver.GetAppDataPath("userprefs.json"), resolver.UserPrefsFilePath);
            Assert.Equal(resolver.GetAppDataPath("userprefs.txt"), resolver.LegacyUserPrefsFilePath);
            Assert.Equal(resolver.GetAppDataPath("players-cache.json"), resolver.PlayerListFilePath);
            Assert.Equal(resolver.GetAppDataPath("logs"), resolver.LogsFolderPath);
        }

        [Fact]
        public void Windows_BinaryName_IsServerExe()
        {
            var resolver = new WindowsValheimPathResolver();
            Assert.Equal("valheim_server.exe", resolver.ServerBinaryName);
        }

        // The OS boundary (environment-variable expansion) is injected, so the exact Windows paths are pinned on
        // every OS rather than only when the suite happens to run on Windows.
        [Fact]
        public void Windows_Paths_AreUnderRuneberryLocalLow()
        {
            static string Expand(string template) => template
                .Replace("%USERPROFILE%", @"C:\Users\Viking")
                .Replace("%ProgramFiles(x86)%", @"C:\Program Files (x86)");
            var resolver = new WindowsValheimPathResolver(Expand);

            Assert.Equal(@"C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server\valheim_server.exe",
                resolver.DefaultServerPath);
            Assert.Equal(@"C:\Users\Viking\AppData\LocalLow\IronGate\Valheim", resolver.DefaultSaveDataFolder);
            Assert.Equal(Path.Join(@"C:\Users\Viking\AppData\LocalLow\Runeberry\ValheimServerGUI", "userprefs.json"),
                resolver.UserPrefsFilePath);
        }
    }
}
