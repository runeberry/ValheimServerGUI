using System;
using System.IO;
using System.Linq;
using Serilog;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// Cloud-world import round-trip against Nuffle's real Steam Cloud saves: enumerate the in-game worlds
    /// under the configured Steam root and import one (copy-only) into a fresh mochi-owned savedir, then
    /// assert it lists as a local world. Copy-only (never move) leaves Nuffle's cloud data untouched.
    /// </summary>
    public sealed class CloudWorldImportTests
    {
        // The known in-game (Steam Cloud) world on this machine (see scripts/integration.local.env).
        private const string CloudWorld = "TWReleaseWorld";

        /// <summary>
        /// Points the cloud provider at the configured Steam root instead of mochi's $HOME (the Linux
        /// resolver would otherwise look under /home/mochi, not Nuffle's real Steam install).
        /// </summary>
        private sealed class FixedSteamPathResolver : ISteamPathResolver
        {
            private readonly string _root;
            public FixedSteamPathResolver(string root) => _root = root;
            public string? GetSteamInstallPath() => _root;
        }

        [Fact]
        public void ImportCloudWorld_CopyRoundTrip_ListsLocally()
        {
            IntegrationConfig.SkipIfSteamUnconfigured();

            ILogger logger = new LoggerConfiguration().CreateLogger();
            var provider = new SteamCloudWorldProvider(logger, new FixedSteamPathResolver(IntegrationConfig.SteamRoot!));

            var cloudNames = provider.GetCloudWorldNames().ToList();
            Assert.Contains(CloudWorld, cloudNames);

            // Fresh dest under the mochi-owned savedir; copy-only so the Steam Cloud source is never deleted.
            var dest = new DirectoryInfo(Path.Join(IntegrationConfig.SaveDir, "cloud-import-" + Guid.NewGuid().ToString("n")[..6]));
            dest.Create();
            Assert.True(dest.IsWorldNameAvailable(CloudWorld), "Dest should start without the world.");

            provider.ImportCloudWorld(CloudWorld, dest, move: false);

            Assert.False(dest.IsWorldNameAvailable(CloudWorld), "World should be present locally after import.");
            Assert.Contains(CloudWorld, dest.GetWorldNames());
        }
    }
}
