using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// ValheimServer orchestration via a headless process provider (E1/E5/E6/E7, §5.1-5.4). Status
    /// transitions, the GenerateArgs launch contract, the late-stop race, graceful-stop dispatch, and
    /// the restart choreography -- all with no real process, exe launch, or filesystem beyond a
    /// throwaway temp exe/savedir the validation requires.
    /// </summary>
    public class ValheimServerTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _exe;
        private readonly string _saveDir;
        private readonly MockProcessProvider _processProvider;
        private readonly ValheimServer _server;

        public ValheimServerTests()
        {
            _dir = Path.Join(Path.GetTempPath(), "vsg-server-" + Guid.NewGuid().ToString("n"));
            _saveDir = Path.Join(_dir, "save");
            Directory.CreateDirectory(_saveDir);
            _exe = Path.Join(_dir, "valheim_server.x86_64");
            File.WriteAllText(_exe, ""); // validation only needs the file to exist

            var fileProvider = new MockDataFileProvider();
            ILogger serilog = new LoggerConfiguration().CreateLogger();
            var context = new DataFileRepositoryContext(fileProvider, serilog);
            var resolver = new LinuxValheimPathResolver("/tmp/vsg-test-home", xdgDataHome: null);
            var repo = new PlayerDataRepository(context, new FakeRuneberryApiClient(), resolver);

            _processProvider = new MockProcessProvider();
            _server = new ValheimServer(_processProvider, repo, new FakeApplicationLogger(), resolver, new PlayerAccessListService());
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        private ValheimServerOptions Options(Action<ValheimServerOptions>? customize = null)
        {
            var options = new ValheimServerOptions
            {
                Name = "My Server",
                WorldName = "MyWorld",
                Password = "hunter2",
                Port = 2456,
                SaveInterval = 30,
                Backups = 1,
                BackupShort = 60,
                BackupLong = 120,
                ServerExePath = _exe,
                SaveDataFolderPath = _saveDir,
                LogToFile = false,
            };
            customize?.Invoke(options);
            return options;
        }

        private void FeedLog(string line) => _server.Logger!.Information(line);

        private static void WaitFor(Func<bool> condition, int timeoutMs = 2000)
        {
            var sw = Stopwatch.StartNew();
            while (!condition() && sw.ElapsedMilliseconds < timeoutMs) Thread.Sleep(10);
        }

        [Fact]
        public void IsStoppedInitially()
        {
            Assert.Equal(ServerStatus.Stopped, _server.Status);
            Assert.True(_server.CanStart);
            Assert.False(_server.CanStop);
        }

        [Fact]
        public void Start_MovesToStarting_AndBlocksReStart()
        {
            _server.Start(Options());

            Assert.Equal(ServerStatus.Starting, _server.Status);
            Assert.False(_server.CanStart);
            Assert.True(_server.CanStop);
        }

        // §5.3: the GenerateArgs launch contract.
        [Fact]
        public void Start_EmitsExpectedLaunchArgs()
        {
            _server.Start(Options(o =>
            {
                o.Public = true;
                o.Crossplay = true;
                o.WorldPreset = WorldGenPresets.Hard;
            }));

            var args = _processProvider.LastProcess!.StartInfo.Arguments;

            Assert.Contains("-nographics -batchmode", args);
            Assert.Contains("-name \"My Server\"", args);
            Assert.Contains("-port 2456", args);
            Assert.Contains("-world \"MyWorld\"", args);
            Assert.Contains("-public 1", args);
            Assert.Contains("-password \"hunter2\"", args);
            Assert.Contains("-crossplay", args);
            Assert.Contains("-preset hard", args);
        }

        // Linux launch contract: the server binary resolves steamclient.so from its own directory
        // (and linux64/ beneath it) and reads steam_appid.txt from the working directory, so Start
        // must set the working directory to the exe's folder and prepend that folder + linux64/ to
        // LD_LIBRARY_PATH. Without this the raw valheim_server.x86_64 fails to start on Linux.
        [Fact]
        public void Start_SetsWorkingDirectoryAndLibraryPath_ForServerLaunch()
        {
            _server.Start(Options());

            var startInfo = _processProvider.LastProcess!.StartInfo;

            Assert.Equal(CoreConstants.ValheimSteamAppId, startInfo.EnvironmentVariables["SteamAppId"]);
            Assert.Equal(_dir, startInfo.WorkingDirectory);

            // LD_LIBRARY_PATH is only set off-Windows; on Windows the DLL search handles this itself.
            if (!OperatingSystem.IsWindows())
            {
                var libPath = startInfo.EnvironmentVariables["LD_LIBRARY_PATH"];
                Assert.NotNull(libPath);
                Assert.Contains(_dir, libPath!.Split(':'));
                Assert.Contains(Path.Combine(_dir, "linux64"), libPath.Split(':'));
            }
        }

        [Fact]
        public void ConnectedLogLine_PromotesToRunning()
        {
            _server.Start(Options());

            FeedLog("Game server connected");

            Assert.Equal(ServerStatus.Running, _server.Status);
            Assert.True(_server.CanRestart);
        }

        [Fact]
        public void Stop_DispatchesGracefulKill_AndEntersStopping()
        {
            _server.Start(Options());

            _server.Stop();

            Assert.Equal(ServerStatus.Stopping, _server.Status);
            Assert.Single(_processProvider.SafelyKilledKeys);
        }

        // §16.2 stop-timeout: a graceful stop that never completes gets force-killed after the timeout.
        [Fact]
        public void Stop_ThatHangs_ForceKillsAfterTimeout()
        {
            _server.GracefulStopTimeout = TimeSpan.FromMilliseconds(100);
            var timedOut = false;
            _server.StopTimedOut += (_, _) => timedOut = true;

            _server.Start(Options());
            _server.Stop();
            // Simulate a hung process: never raise Exited.

            WaitFor(() => _processProvider.ForceKilledKeys.Count > 0);

            Assert.Single(_processProvider.ForceKilledKeys);
            Assert.True(timedOut);
        }

        // A/B for the above: when the process exits gracefully before the timeout, no force-kill happens.
        [Fact]
        public void Stop_ThatCompletes_DoesNotForceKill()
        {
            _server.GracefulStopTimeout = TimeSpan.FromMilliseconds(100);

            _server.Start(Options());
            _server.Stop();
            _processProvider.SimulateExit(); // graceful shutdown completes

            Thread.Sleep(300); // past the timeout window

            Assert.Empty(_processProvider.ForceKilledKeys);
            Assert.Equal(ServerStatus.Stopped, _server.Status);
        }

        // E6: stop during startup; a late "connected" must not promote Stopping -> Running.
        [Fact]
        public void LateConnected_AfterStop_StaysStopping()
        {
            _server.Start(Options());
            _server.Stop();
            Assert.Equal(ServerStatus.Stopping, _server.Status);

            FeedLog("Game server connected");

            Assert.Equal(ServerStatus.Stopping, _server.Status);
        }

        // E5: the process dies unexpectedly -> Stopped, no auto-restart.
        [Fact]
        public void ProcessExit_MovesToStopped_AndAllowsRestart()
        {
            _server.Start(Options());

            _processProvider.SimulateExit();

            Assert.Equal(ServerStatus.Stopped, _server.Status);
            Assert.True(_server.CanStart);
        }

        [Fact]
        public void Restart_WhenRunning_DispatchesKill_AndEntersStopping()
        {
            _server.Start(Options());
            FeedLog("Game server connected");
            Assert.Equal(ServerStatus.Running, _server.Status);

            _server.Restart();

            Assert.Equal(ServerStatus.Stopping, _server.Status);
            Assert.Single(_processProvider.SafelyKilledKeys);
        }

        // E7: a restart re-starts the server once the old process has exited.
        [Fact]
        public void Restart_AfterProcessExits_StartsAgain()
        {
            _server.Start(Options());
            FeedLog("Game server connected");
            _server.Restart();

            _processProvider.SimulateExit();

            // The Stopped handler waits ~500ms before re-starting.
            WaitFor(() => _server.Status == ServerStatus.Starting);
            Assert.Equal(ServerStatus.Starting, _server.Status);
        }

        // StartedAt derives uptime from the server's own Running transition (not a per-window capture), and
        // clears once fully Stopped so a re-target onto a stopped profile reads no uptime.
        [Fact]
        public void StartedAt_IsStampedOnRunning_AndClearedOnStopped()
        {
            Assert.Null(_server.StartedAt);

            _server.Start(Options());
            Assert.Null(_server.StartedAt); // Starting, not yet Running

            FeedLog("Game server connected");
            Assert.NotNull(_server.StartedAt);

            _processProvider.SimulateExit(); // → Stopped
            Assert.Null(_server.StartedAt);
        }

        // The start path projects the profile's roles onto the three gating files (one hook covers manual /
        // auto / restart). Here: permitted-list mode puts the admin on both adminlist and permittedlist, and
        // the ban list is regenerated header-only (ignored in this mode).
        [Fact]
        public void Start_GeneratesAccessListsFromOptions()
        {
            _server.Start(Options(o =>
            {
                o.UsePermittedList = true;
                o.PlayerRoles = new[]
                {
                    new PlayerRoleAssignment(PlayerPlatforms.Steam, PlayerPlatforms.Steam, "111", PlayerRole.Admin),
                    new PlayerRoleAssignment(PlayerPlatforms.Steam, PlayerPlatforms.Steam, "222", PlayerRole.Banned),
                };
            }));

            var admin = File.ReadAllLines(Path.Join(_saveDir, "adminlist.txt"));
            var permitted = File.ReadAllLines(Path.Join(_saveDir, "permittedlist.txt"));
            var banned = File.ReadAllLines(Path.Join(_saveDir, "bannedlist.txt"));

            Assert.Contains("Steam_111", admin);
            Assert.Contains("Steam_111", permitted);          // admin must also be permitted to join
            Assert.DoesNotContain("Steam_222", banned);       // ban list unused in permitted mode
            Assert.Single(banned);                            // header only
        }

        // SkipAccessListGeneration (set by the start-time "use roles from file" choice) leaves whatever is on
        // disk untouched, whereas the default regenerates. A/B over the same seeded file proves the flag gates
        // generation rather than the probe never reaching the write.
        [Fact]
        public void Start_SkipAccessListGeneration_LeavesFilesUntouched()
        {
            var adminPath = Path.Join(_saveDir, "adminlist.txt");
            File.WriteAllText(adminPath, "// header\nSteam_999\n"); // a manual entry not in any role

            _server.Start(Options(o => o.SkipAccessListGeneration = true));

            Assert.Contains("Steam_999", File.ReadAllLines(adminPath)); // preserved verbatim
        }

        [Fact]
        public void Start_WithoutSkip_RegeneratesAndOverwritesFiles()
        {
            var adminPath = Path.Join(_saveDir, "adminlist.txt");
            File.WriteAllText(adminPath, "// header\nSteam_999\n"); // manual entry, no matching role

            _server.Start(Options()); // SkipAccessListGeneration defaults false → header-only regeneration

            Assert.DoesNotContain("Steam_999", File.ReadAllLines(adminPath)); // overwritten
        }

        // The live carve-out: applying roles to an already-running server regenerates the files now AND
        // updates the live options, so a restart (which reuses Options) keeps the change instead of reverting.
        [Fact]
        public void ApplyPlayerRoles_RegeneratesFiles_AndUpdatesLiveOptions()
        {
            _server.Start(Options()); // started with no roles -> header-only files
            Assert.Single(File.ReadAllLines(Path.Join(_saveDir, "adminlist.txt")));

            var roles = new[]
            {
                new PlayerRoleAssignment(PlayerPlatforms.Steam, PlayerPlatforms.Steam, "500", PlayerRole.Admin),
            };
            _server.ApplyPlayerRoles(roles, usePermittedList: false);

            Assert.Contains("Steam_500", File.ReadAllLines(Path.Join(_saveDir, "adminlist.txt")));
            // Options updated so a subsequent restart regenerates from the new roles, not the start-time ones.
            Assert.Same(roles, _server.Options.PlayerRoles);
        }

        [Fact]
        public void WorldSavedLogLine_ReRaisesWorldSavedEvent()
        {
            decimal? saved = null;
            _server.WorldSaved += (_, ms) => saved = ms;
            _server.Start(Options());

            FeedLog("World saved ( 10.5ms )");

            Assert.Equal(10.5m, saved);
        }
    }
}
