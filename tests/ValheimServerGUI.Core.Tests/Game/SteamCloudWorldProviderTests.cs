using System;
using System.IO;
using System.Linq;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// <see cref="SteamCloudWorldProvider"/> against a fake Steam install built in a temp folder:
    /// <c>userdata/&lt;account&gt;/892970/remote/worlds/&lt;World&gt;/</c>, holding a real game-written save fixture.
    /// </summary>
    public class SteamCloudWorldProviderTests : IDisposable
    {
        private const string AccountA = "11111111";
        private const string AccountB = "22222222";

        private readonly DirectoryInfo _root;
        private readonly DirectoryInfo _steam;
        private readonly DirectoryInfo _localSaves;

        public SteamCloudWorldProviderTests()
        {
            _root = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_cloud_" + Guid.NewGuid().ToString("N")));
            _steam = new DirectoryInfo(Path.Join(_root.FullName, "Steam"));
            _localSaves = new DirectoryInfo(Path.Join(_root.FullName, "saves"));
            _steam.Create();
            _localSaves.Create();
        }

        public void Dispose()
        {
            if (_root.Exists) _root.Delete(true);
        }

        private sealed class FixedSteamPath(string? root) : ISteamPathResolver
        {
            public string? GetSteamInstallPath() => root;
        }

        private SteamCloudWorldProvider NewProvider(string? steamRoot = null)
            => new(new LoggerConfiguration().CreateLogger(), new FixedSteamPath(steamRoot ?? _steam.FullName));

        private string RemoteWorlds(string account)
            => Path.Join(_steam.FullName, "userdata", account, CoreConstants.ValheimSteamAppId, "remote", "worlds");

        /// <summary>Writes a Valheim 1.0+ cloud world: the real save fixture plus a companion database file.</summary>
        private DirectoryInfo WriteCloudWorld(string account, string worldName)
        {
            var folder = Directory.CreateDirectory(Path.Join(RemoteWorlds(account), worldName));
            File.WriteAllBytes(Path.Join(folder.FullName, "_main.1.fwl2"), WorldSaveFixtures.ReadFixture(WorldSaveFixtures.WithGlobalKey));
            File.WriteAllBytes(Path.Join(folder.FullName, "_main.1.db2"), new byte[] { 1, 2, 3, 4 });
            return folder;
        }

        private static string[] FileNames(DirectoryInfo folder)
            => folder.GetFiles().Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        [Fact]
        public void Lists_each_cloud_world_once_across_accounts()
        {
            WriteCloudWorld(AccountA, "Midgard");
            WriteCloudWorld(AccountA, "Ashlands");
            WriteCloudWorld(AccountB, "Midgard"); // the same world under another Steam account

            var names = NewProvider().GetCloudWorldNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(new[] { "Ashlands", "Midgard" }, names);
        }

        [Fact]
        public void Legacy_fwl_cloud_worlds_are_not_listed_because_they_cannot_be_imported()
        {
            Directory.CreateDirectory(RemoteWorlds(AccountA));
            File.WriteAllText(Path.Join(RemoteWorlds(AccountA), "OldWorld.fwl"), string.Empty);

            Assert.Empty(NewProvider().GetCloudWorldNames());
        }

        [Fact]
        public void Without_a_Steam_install_there_are_no_cloud_worlds()
        {
            Assert.Empty(new SteamCloudWorldProvider(new LoggerConfiguration().CreateLogger(), new FixedSteamPath(null))
                .GetCloudWorldNames());
            Assert.Empty(NewProvider(Path.Join(_root.FullName, "missing")).GetCloudWorldNames());
        }

        [Fact]
        public void Copy_import_brings_every_file_into_worlds_local_and_keeps_the_source()
        {
            var source = WriteCloudWorld(AccountA, "Midgard");
            var provider = NewProvider();
            Assert.True(_localSaves.IsWorldNameAvailable("Midgard"));

            var dest = provider.ImportCloudWorld("Midgard", _localSaves, move: false);

            Assert.Equal(Path.Join(_localSaves.FullName, "worlds_local", "Midgard"), dest.FullName);
            Assert.Equal(FileNames(source), FileNames(dest));
            Assert.Equal(WorldSaveFixtures.ReadFixture(WorldSaveFixtures.WithGlobalKey),
                File.ReadAllBytes(Path.Join(dest.FullName, "_main.1.fwl2")));
            Assert.Contains("Midgard", _localSaves.GetWorldNames());
            Assert.True(source.Exists);
            Assert.Contains("Midgard", provider.GetCloudWorldNames());
        }

        [Fact]
        public void Move_import_removes_the_cloud_source()
        {
            var source = WriteCloudWorld(AccountA, "Midgard");

            NewProvider().ImportCloudWorld("Midgard", _localSaves, move: true);

            Assert.Contains("Midgard", _localSaves.GetWorldNames());
            Assert.False(Directory.Exists(source.FullName));
        }

        [Fact]
        public void Import_refuses_a_world_that_already_exists_locally()
        {
            WriteCloudWorld(AccountA, "Midgard");
            var local = Directory.CreateDirectory(Path.Join(_localSaves.FullName, "worlds_local", "Midgard"));
            File.WriteAllText(Path.Join(local.FullName, "_main.7.fwl2"), "local save");

            Assert.Throws<IOException>(() => NewProvider().ImportCloudWorld("Midgard", _localSaves, move: false));
            Assert.Equal(new[] { "_main.7.fwl2" }, FileNames(local));
        }
    }
}
