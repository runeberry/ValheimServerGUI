using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// Boots the real dedicated server ONCE, captures its log, and gracefully stops it (SIGINT), recording
    /// everything the lifecycle facts assert against. A single boot covers boot→Running, the graceful
    /// save-flush (the A half of E9), and the captured-log fixture-drift check. The force-kill B half lives
    /// in <see cref="ForceKillTests"/> because it must kill a freshly booted server instead of stopping it.
    /// </summary>
    public sealed class LiveLifecycleFixture : IAsyncLifetime
    {
        // First boot does world-gen + a real Steam relay connect; size the timeout for a cold start.
        private static readonly TimeSpan BootTimeout = TimeSpan.FromSeconds(150);
        // The stop is sent right after Running, while a new world may still be generating. Windows defers the
        // graceful stop until generation finishes (34 s measured on a laptop), so allow as long as a cold boot.
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(150);
        // The invite code comes once world generation is done and PlayFab has registered the session.
        private static readonly TimeSpan InviteCodeTimeout = TimeSpan.FromSeconds(120);
        private static readonly TimeSpan SettleAfterRunning = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DrainAfterStop = TimeSpan.FromSeconds(2);

        public bool ReachedRunning { get; private set; }
        public bool StoppedCleanly { get; private set; }
        public string? InviteCode { get; private set; }
        public DateTime? SaveTimeBeforeStopUtc { get; private set; }
        public DateTime? SaveTimeAfterStopUtc { get; private set; }
        public int WorldSavesBeforeStop { get; private set; }
        public int WorldSavesAfterStop { get; private set; }
        public string? ArtifactPath { get; private set; }
        public string? CommittedSaveAfterStop { get; private set; }
        public IReadOnlyList<WorldPlayerHistoryEntry>? HistoryAfterStop { get; private set; }
        public Exception? HistoryReadError { get; private set; }
        public IReadOnlyList<string> CapturedLog { get; private set; } = Array.Empty<string>();

        public async ValueTask InitializeAsync()
        {
            var worldName = "ITLife" + Guid.NewGuid().ToString("n")[..6];
            using var harness = new LiveServerHarness(IntegrationConfig.Current.SaveDir, worldName);

            ReachedRunning = await harness.BootToRunningAsync(BootTimeout);

            if (ReachedRunning)
            {
                InviteCode = await harness.WaitForInviteCodeAsync(InviteCodeTimeout);

                // Let the initial post-gen save settle so the "before" mtime is stable.
                await Task.Delay(SettleAfterRunning);
                SaveTimeBeforeStopUtc = harness.LatestWorldSaveTimeUtc();
                WorldSavesBeforeStop = harness.WorldSaves.Count;

                StoppedCleanly = await harness.StopGracefullyAsync(StopTimeout);

                // Let the final shutdown lines drain through the stdout->parser pipeline before
                // snapshotting: the process's Exited (→ Stopped) can race the last OutputDataReceived.
                await Task.Delay(DrainAfterStop);

                SaveTimeAfterStopUtc = harness.LatestWorldSaveTimeUtc();
                WorldSavesAfterStop = harness.WorldSaves.Count;

                var worldFolder = new DirectoryInfo(harness.SaveDir).GetLocalWorldFolder(worldName);
                CommittedSaveAfterStop = WorldPlayerHistory.FindLatestCompleteSave(worldFolder)?.FullName;
                try { HistoryAfterStop = WorldPlayerHistory.ReadFromWorldFolder(worldFolder); }
                catch (Exception e) { HistoryReadError = e; }
            }

            CapturedLog = harness.Log.ToArray();
            ArtifactPath = WriteArtifact(worldName, CapturedLog);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static string WriteArtifact(string worldName, IReadOnlyList<string> log)
        {
            var path = Path.Join(IntegrationConfig.Current.ArtifactDir,
                $"live-server-{worldName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
            File.WriteAllLines(path, log);
            return path;
        }
    }

    public sealed class LiveLifecycleTests : IClassFixture<LiveLifecycleFixture>
    {
        private readonly LiveLifecycleFixture _fixture;

        public LiveLifecycleTests(LiveLifecycleFixture fixture) => _fixture = fixture;

        private static string Tail(IReadOnlyList<string> log, int n = 30) =>
            string.Join(Environment.NewLine, log.TakeLast(n));

        // E9 / §13: the server reaches Running off the REAL "Game server connected" line, which is what
        // drives the status machine in production. Reaching Running IS the live proof that the parser's
        // connected pattern still matches the current binary's output.
        [Fact]
        public void Boot_ReachesRunning()
        {
            Assert.True(_fixture.ReachedRunning,
                $"Server never reached Running within the boot timeout. Check Steam connectivity. Log tail:{Environment.NewLine}{Tail(_fixture.CapturedLog)}");
        }

        // E9 (A half): a graceful SIGINT stop flushes a world save to disk — proven at the filesystem
        // boundary (the world data file's mtime advances), not just by a log line, and corroborated by the
        // real parser raising a NEW WorldSaved after the stop. ForceKillTests is the B half that shows a
        // hard kill does NOT advance the save, making this path meaningful.
        [Fact]
        public void GracefulStop_FlushesWorldSave()
        {
            Assert.True(_fixture.ReachedRunning, "Precondition failed: server never reached Running.");
            Assert.True(_fixture.StoppedCleanly, "Server did not reach Stopped within the graceful-stop timeout.");

            Assert.NotNull(_fixture.SaveTimeAfterStopUtc);
            var advanced = _fixture.SaveTimeBeforeStopUtc is null
                || _fixture.SaveTimeAfterStopUtc > _fixture.SaveTimeBeforeStopUtc;
            Assert.True(advanced,
                $"World save mtime did not advance on graceful stop (before={_fixture.SaveTimeBeforeStopUtc:o}, after={_fixture.SaveTimeAfterStopUtc:o}).");

            Assert.True(_fixture.WorldSavesAfterStop > _fixture.WorldSavesBeforeStop,
                $"No new 'World saved' event was parsed during the graceful stop (before={_fixture.WorldSavesBeforeStop}, after={_fixture.WorldSavesAfterStop}).");
        }

        // Save-format drift: the world save the real binary just wrote must still parse with the exact layout the
        // player-name reader expects (it fails on any byte that does not line up). Nobody joined, so the history is
        // empty. A Valheim save-format change surfaces here before it can misname players.
        [Fact]
        public void GracefulStop_WritesAWorldSaveThePlayerHistoryReaderParses()
        {
            Assert.True(_fixture.StoppedCleanly, "Precondition failed: server did not stop cleanly.");

            Assert.NotNull(_fixture.CommittedSaveAfterStop);
            Assert.Null(_fixture.HistoryReadError);
            Assert.NotNull(_fixture.HistoryAfterStop);
            Assert.Empty(_fixture.HistoryAfterStop!);
        }

        // Mods load when VSG starts the server, and the user sees it in the server log: with BepInEx installed, the
        // preloader runs inside the process VSG launched and the chainloader loads the installed plugin; on a vanilla
        // install no mod loader appears at all. On Linux this needs VSG to set up the loader's environment from the
        // configured paths (bepinex-external keeps the pack outside the server folder), and the lines arrive on
        // stdout. On Windows BepInEx's console logger never reaches stdout, so the plugin lines arrive only through
        // VSG's tail of BepInEx's LogOutput.log, which must not repeat what stdout already shows.
        [Fact]
        public void ModLoader_MatchesServerConfiguration()
        {
            Assert.True(_fixture.ReachedRunning, "Precondition failed: server never reached Running.");
            var log = _fixture.CapturedLog;
            var config = IntegrationConfig.Current;

            if (config.ServerConfig == ServerConfiguration.BepInEx)
            {
                Assert.True(log.Any(l => l.Contains("Preloader finished")),
                    $"The BepInEx preloader did not run in the server VSG started. Artifact: {_fixture.ArtifactPath}");

                foreach (var plugin in config.ExpectedPlugins)
                {
                    Assert.True(log.Count(l => l.Contains($"Loading [{plugin}]")) == 1,
                        $"The server log does not show BepInEx loading the plugin '{plugin}' exactly once. Artifact: {_fixture.ArtifactPath}");
                }
                var loaded = $"Chainloader startup complete ({config.ExpectedPlugins.Count} loaded, 0 skipped, 0 failed)";
                Assert.True(log.Count(l => l.Contains(loaded)) == 1,
                    $"The server log does not show '{loaded}' exactly once. Artifact: {_fixture.ArtifactPath}");
                if (OperatingSystem.IsWindows())
                {
                    Assert.True(log.Count(l => l.Contains("Preloader finished")) == 1,
                        $"The preloader lines BepInEx replays to stdout were repeated from LogOutput.log. Artifact: {_fixture.ArtifactPath}");
                }
            }
            else
            {
                Assert.DoesNotContain(log, l => l.Contains("BepInEx"));
            }
        }

        // Crossplay (a new profile's default): the server registers a PlayFab session and VSG picks the invite code
        // players join with out of the live log. Runs with the configuration's mods loaded, so a mod that breaks
        // crossplay registration fails here.
        [Fact]
        public void Crossplay_InviteCodeAppears()
        {
            Assert.True(_fixture.ReachedRunning, "Precondition failed: server never reached Running.");
            Assert.True(_fixture.InviteCode is not null,
                $"No invite code was raised from the server log. Artifact: {_fixture.ArtifactPath}");
            Assert.Matches("^[0-9]{6}$", _fixture.InviteCode);
        }

        // Fixture-drift / capture: the captured real log is written as a run artifact, and we re-feed it
        // through a fresh REAL ServerLogParser to confirm the current binary still emits lines the parser's
        // patterns match (connected + world-saved). A Valheim log-format change surfaces here; refreshing the
        // tier-2 inline fixtures in ServerLogParserTests stays a manual follow-up.
        [Fact]
        public void CapturedLog_StillMatchesParserPatterns()
        {
            Assert.NotNull(_fixture.ArtifactPath);
            Assert.True(File.Exists(_fixture.ArtifactPath), $"Captured-log artifact missing: {_fixture.ArtifactPath}");
            Assert.NotEmpty(_fixture.CapturedLog);

            using var services = LiveServerHarness.BuildSandboxedCore(IntegrationConfig.Current.SaveDir);
            var parser = new ServerLogParser(
                services.GetRequiredService<IPlayerDataRepository>(),
                services.GetRequiredService<IApplicationLogger>());

            var connected = false;
            var saved = false;
            parser.ServerConnected += (_, _) => connected = true;
            parser.WorldSaved += (_, _) => saved = true;

            foreach (var line in _fixture.CapturedLog)
            {
                parser.ProcessLine(line);
            }

            Assert.True(connected,
                $"ServerLogParser no longer matches the real 'connected' line — Valheim log format may have changed. Artifact: {_fixture.ArtifactPath}");
            Assert.True(saved,
                $"ServerLogParser no longer matches the real 'World saved' line — refresh the tier-2 fixtures. Artifact: {_fixture.ArtifactPath}");
        }
    }
}
