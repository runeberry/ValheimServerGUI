using System;
using System.Collections.Generic;
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
        private readonly PlayerDataRepository _repo;
        private readonly FakeApplicationLogger _appLogger = new();
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
            _repo = new PlayerDataRepository(context, resolver);

            _processProvider = new MockProcessProvider();
            var accessLists = new PlayerAccessListService();
            var baselines = new InMemoryBaselineStore();
            _server = new ValheimServer(_processProvider, _repo, _appLogger, resolver, accessLists,
                new PlayerListImportService(accessLists, baselines), baselines);
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

            // The timeout task force-kills and THEN raises StopTimedOut, so wait for both: waiting for the kill alone
            // let the assertion below land between the two under load.
            WaitFor(() => _processProvider.ForceKilledKeys.Count > 0 && timedOut);

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

        private static Dictionary<string, PlayerRoleEntry> Overrides(params (string id, PlayerRole role)[] roles)
        {
            var result = new Dictionary<string, PlayerRoleEntry>();
            foreach (var (id, role) in roles) result[$"Steam:{id}"] = new PlayerRoleEntry(role, "Steam");
            return result;
        }

        private string[] ListLines(string file) => File.ReadAllLines(Path.Join(_saveDir, file));

        // The start path writes the profile's roles to the three gating files (one hook covers manual / auto /
        // restart). Here: permitted-list mode puts the admin on both adminlist and permittedlist, and the ban list
        // is header-only (ignored in this mode).
        [Fact]
        public void Start_WritesAccessListsFromOptions()
        {
            _server.Start(Options(o =>
            {
                o.UsePermittedList = true;
                o.RoleOverrides = Overrides(("111", PlayerRole.Admin), ("222", PlayerRole.Banned));
            }));

            Assert.Contains("Steam_111", ListLines("adminlist.txt"));
            Assert.Contains("Steam_111", ListLines("permittedlist.txt")); // admin must also be permitted to join
            Assert.DoesNotContain("Steam_222", ListLines("bannedlist.txt")); // ban list unused in permitted mode
            Assert.Single(ListLines("bannedlist.txt"));                      // header only
        }

        // Effective roles are resolved from the overrides over the global defaults: a default reaches the files
        // unless this server overrides it.
        [Fact]
        public void Start_ResolvesOverridesOverDefaults()
        {
            _server.Start(Options(o =>
            {
                o.RoleOverrides = Overrides(("111", PlayerRole.Permitted));
                o.RoleDefaults = new Dictionary<string, PlayerDefaultEntry>
                {
                    ["Steam:111"] = new(PlayerRole.Admin, "Steam"),
                    ["Steam:222"] = new(PlayerRole.Admin, "Steam"),
                };
            }));

            Assert.DoesNotContain("Steam_111", ListLines("adminlist.txt")); // overridden
            Assert.Contains("Steam_222", ListLines("adminlist.txt"));       // default applies
        }

        // An entry the profile doesn't have is dropped from the file, but only after the file is backed up.
        [Fact]
        public void Start_EntryNotInTheProfile_IsBackedUpBeforeItIsDropped()
        {
            File.WriteAllText(Path.Join(_saveDir, "adminlist.txt"), "// header\nSteam_76561198000000999\n");

            _server.Start(Options());

            Assert.DoesNotContain("Steam_76561198000000999", ListLines("adminlist.txt"));
            Assert.Contains("Steam_76561198000000999", ListLines("adminlist.bak.txt"));
        }

        // The live carve-out: applying roles to an already-running server rewrites the files now AND updates the
        // live options, so a restart (which reuses Options) keeps the change instead of reverting.
        [Fact]
        public void ApplyPlayerRoles_RewritesFiles_AndUpdatesLiveOptions()
        {
            _server.Start(Options()); // started with no roles -> header-only files
            Assert.Single(ListLines("adminlist.txt"));

            var overrides = Overrides(("500", PlayerRole.Admin));
            _server.ApplyPlayerRoles(overrides, new Dictionary<string, PlayerDefaultEntry>(), usePermittedList: false);

            Assert.Contains("Steam_500", ListLines("adminlist.txt"));
            Assert.Same(overrides, _server.Options.RoleOverrides);
        }

        // A change made to the files during play (here: an in-game ban) is adopted before a live role change
        // rewrites them, and the owner is told so it can save the adopted roles to the profile.
        [Fact]
        public void ApplyPlayerRoles_AdoptsChangesMadeDuringPlay_AndRaisesPlayerRolesAdopted()
        {
            _server.Start(Options());
            FeedLog("Game server connected");
            File.AppendAllText(Path.Join(_saveDir, "bannedlist.txt"), "Steam_76561198000000777\n");
            ReconcileResult? adopted = null;
            _server.PlayerRolesAdopted += (_, r) => adopted = r;

            _server.ApplyPlayerRoles(Overrides(("500", PlayerRole.Admin)), new Dictionary<string, PlayerDefaultEntry>(), false);

            Assert.Contains("Steam_76561198000000777", ListLines("bannedlist.txt"));
            Assert.NotNull(adopted);
            Assert.Equal(PlayerRole.Banned, adopted!.Overrides["Steam:76561198000000777"].Role);
            Assert.Equal(PlayerRole.Admin, adopted.Overrides["Steam:500"].Role); // the live change itself is kept
        }

        [Fact]
        public void Restart_AdoptsChangesMadeDuringPlay()
        {
            _server.Start(Options());
            FeedLog("Game server connected");
            File.AppendAllText(Path.Join(_saveDir, "bannedlist.txt"), "Steam_76561198000000777\n");
            ReconcileResult? adopted = null;
            _server.PlayerRolesAdopted += (_, r) => adopted = r;

            _server.Restart();
            _processProvider.SimulateExit();
            WaitFor(() => _server.Status == ServerStatus.Starting);

            Assert.Contains("Steam_76561198000000777", ListLines("bannedlist.txt"));
            Assert.NotNull(adopted);
            Assert.Equal(PlayerRole.Banned, _server.Options.RoleOverrides["Steam:76561198000000777"].Role);
        }

        // A plain start does NOT adopt (the view-model reconciles first, asking the user about conflicts): an
        // unknown entry is dropped, backed up — the profile wins.
        [Fact]
        public void Start_DoesNotAdopt_ItBacksUpInstead()
        {
            File.WriteAllText(Path.Join(_saveDir, "bannedlist.txt"), "// header\nSteam_76561198000000777\n");
            var raised = false;
            _server.PlayerRolesAdopted += (_, _) => raised = true;

            _server.Start(Options());

            Assert.False(raised);
            Assert.DoesNotContain("Steam_76561198000000777", ListLines("bannedlist.txt"));
            Assert.Contains("Steam_76561198000000777", ListLines("bannedlist.bak.txt"));
        }

        // A list backup that fails stops the launch before anything starts.
        [Fact]
        public void Start_WhenAListBackupFails_DoesNotStart()
        {
            File.WriteAllText(Path.Join(_saveDir, "adminlist.txt"), "// header\nSteam_76561198000000999\n");
            Directory.CreateDirectory(Path.Join(_saveDir, "adminlist.bak.txt"));
            for (var i = 2; i <= 5; i++) Directory.CreateDirectory(Path.Join(_saveDir, $"adminlist.bak.{i}.txt"));

            Assert.Throws<PlayerListBackupException>(() => _server.Start(Options()));

            Assert.Equal(ServerStatus.Stopped, _server.Status);
            Assert.True(_server.CanStart);
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

        // Player names from the world save: read at start, reread after each save, and never a reason not to start.

        private DirectoryInfo WorldFolder => new DirectoryInfo(_saveDir).GetLocalWorldFolder("MyWorld");

        private static byte[] SaveNaming(params (string Id, string Name)[] players)
            => WorldSaveFixtures.Build(players.Select(p => new WorldPlayerHistoryEntry(p.Id, p.Name, p.Name, "")));

        private PlayerInfo KnownPlayer(string platform, string playerId)
        {
            var player = new PlayerInfo { Platform = platform, PlayerId = playerId };
            _repo.Upsert(player);
            return player;
        }

        [Fact]
        public void Start_NamesKnownPlayersFromTheWorldSave()
        {
            var player = KnownPlayer(PlayerPlatforms.Steam, "76561198000000001");
            WorldSaveFixtures.WriteSave(WorldFolder, 3, SaveNaming(("Steam_76561198000000001", "Viking")));

            _server.Start(Options());

            Assert.Equal("Viking", player.PlayerName);
        }

        [Fact]
        public void WorldSaveCompleted_RereadsTheWorldSave()
        {
            var steam = KnownPlayer(PlayerPlatforms.Steam, "76561198000000001");
            var xbox = KnownPlayer(PlayerPlatforms.Xbox, "2533274900000001");
            WorldSaveFixtures.WriteSave(WorldFolder, 3, SaveNaming(("Steam_76561198000000001", "Viking")));
            _server.Start(Options());
            Assert.Null(xbox.PlayerName);

            // The Xbox player joined mid-session; the next save records them.
            WorldSaveFixtures.WriteSave(WorldFolder, 4, SaveNaming(
                ("Steam_76561198000000001", "Viking"),
                ("Xbox_2533274900000001", "Shieldmaiden1")));
            FeedLog("World save (5/5) done. Total time [20ms]");

            Assert.Equal("Viking", steam.PlayerName);
            Assert.Equal("Shieldmaiden1", xbox.PlayerName);
        }

        [Fact]
        public void Start_WithAnUnreadableWorldSave_StillStarts_AndLogsAWarning()
        {
            WorldSaveFixtures.WriteSave(WorldFolder, 3, new byte[] { 0xFF, 0xFF });

            _server.Start(Options());

            Assert.Equal(ServerStatus.Starting, _server.Status);
            Assert.Contains(_appLogger.Messages, m => m.Contains("Could not read player names"));
        }

        [Fact]
        public void Start_WithNoWorldSaveYet_StillStarts()
        {
            _server.Start(Options());

            Assert.Equal(ServerStatus.Starting, _server.Status);
            Assert.DoesNotContain(_appLogger.Messages, m => m.Contains("Could not read player names"));
        }
    }
}
