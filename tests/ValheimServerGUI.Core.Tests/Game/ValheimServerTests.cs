using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Data;
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
            _server = new ValheimServer(_processProvider, repo, new FakeApplicationLogger(), resolver);
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
