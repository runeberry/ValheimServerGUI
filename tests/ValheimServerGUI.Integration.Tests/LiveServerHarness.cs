using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// An <see cref="IProcessProvider"/> decorator over the real <see cref="ProcessProvider"/> that keeps a
    /// handle on the launched server process, so a test can force-kill it (SIGKILL) without going through
    /// <see cref="ValheimServer.Stop"/> (which sends SIGINT). This is the B half of the E9 A/B: a hard kill
    /// must NOT flush a save, which is what makes the graceful SIGINT flush meaningful.
    /// </summary>
    internal sealed class CapturingProcessProvider : IProcessProvider
    {
        private readonly ProcessProvider _inner = new();

        /// <summary>The most recently launched Valheim server process (not the short-lived kill helper).</summary>
        public Process? ServerProcess { get; private set; }

        public void AddProcess(string key, Process process)
        {
            // SafelyKillProcess launches a transient "kill"/"taskkill" helper through this same path;
            // track only the actual server binary so ServerProcess stays the thing we want to kill.
            if (process.StartInfo.FileName.Contains("valheim_server"))
            {
                ServerProcess = process;
            }
            _inner.AddProcess(key, process);
        }

        public Process? GetProcess(string key) => _inner.GetProcess(key);
        public void StartIO(Process process) => _inner.StartIO(process);
        public void SafelyKillProcess(string key) => _inner.SafelyKillProcess(key);
        public void ForceKillProcess(string key) => _inner.ForceKillProcess(key);
    }

    /// <summary>
    /// Drives a REAL <see cref="ValheimServer"/> (built from <see cref="CoreServiceCollectionExtensions.AddValheimCore"/>)
    /// against the real dedicated-server binary: boot to Running off the live "Game server connected" line,
    /// capture the full log stream, and stop either gracefully (SIGINT) or hard (SIGKILL). All server output
    /// goes to the caller-supplied mochi-owned savedir.
    /// </summary>
    internal sealed class LiveServerHarness : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly object _lock = new();
        private TaskCompletionSource<bool>? _runningTcs;

        public CapturingProcessProvider Processes { get; }
        public ValheimServer Server { get; }
        public string WorldName { get; }
        public string SaveDir { get; }

        /// <summary>Every log line the parser sees, captured via <c>options.LogMessageHandler</c>.</summary>
        public List<string> Log { get; } = new();

        /// <summary>World-save durations (ms) as the real parser raised them from the live log.</summary>
        public List<decimal> WorldSaves { get; } = new();

        public LiveServerHarness(string saveDir, string worldName)
        {
            SaveDir = saveDir;
            WorldName = worldName;
            Directory.CreateDirectory(saveDir);

            _services = new ServiceCollection().AddValheimCore().BuildServiceProvider();

            Processes = new CapturingProcessProvider();
            Server = new ValheimServer(
                Processes,
                _services.GetRequiredService<IPlayerDataRepository>(),
                _services.GetRequiredService<IApplicationLogger>(),
                _services.GetRequiredService<IValheimPathResolver>());

            Server.WorldSaved += (_, ms) => { lock (_lock) WorldSaves.Add(ms); };
            Server.StatusChanged += (_, s) => { if (s == ServerStatus.Running) _runningTcs?.TrySetResult(true); };
        }

        public T GetService<T>() where T : notnull => _services.GetRequiredService<T>();

        private ValheimServerOptions BuildOptions() => new()
        {
            Name = "VSG Integration Smoke",
            WorldName = WorldName,
            Password = "vsgsmoke",
            // §13: run public so the server goes through the real Steam relay path to "connected".
            Public = true,
            Port = 2700,
            Crossplay = false,
            // Keep the auto-save interval long so no background save muddies the stop A/B window.
            SaveInterval = 1800,
            Backups = 1,
            BackupShort = 7200,
            BackupLong = 43200,
            ServerExePath = IntegrationConfig.ServerExe,
            SaveDataFolderPath = SaveDir,
            LogToFile = false,
            LogMessageHandler = line => { lock (_lock) Log.Add(line); },
        };

        /// <summary>Starts the server and waits for the real "connected" line to promote it to Running.</summary>
        public async Task<bool> BootToRunningAsync(TimeSpan timeout)
        {
            _runningTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Server.Start(BuildOptions());

            var completed = await Task.WhenAny(_runningTcs.Task, Task.Delay(timeout));
            return completed == _runningTcs.Task && Server.Status == ServerStatus.Running;
        }

        /// <summary>Gracefully stops the server (SIGINT) and waits for it to reach Stopped.</summary>
        public Task<bool> StopGracefullyAsync(TimeSpan timeout)
        {
            return WaitForStopped(timeout, () => Server.Stop());
        }

        /// <summary>Hard-kills the server process (SIGKILL, no save flush) and waits for Stopped.</summary>
        public Task<bool> ForceKillAsync(TimeSpan timeout)
        {
            return WaitForStopped(timeout, () =>
            {
                var proc = Processes.ServerProcess
                    ?? throw new InvalidOperationException("No server process was captured to force-kill.");
                proc.Kill(entireProcessTree: true);
            });
        }

        private async Task<bool> WaitForStopped(TimeSpan timeout, Action initiate)
        {
            var stoppedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnStatus(object? _, ServerStatus s) { if (s == ServerStatus.Stopped) stoppedTcs.TrySetResult(true); }
            Server.StatusChanged += OnStatus;
            try
            {
                initiate();
                var completed = await Task.WhenAny(stoppedTcs.Task, Task.Delay(timeout));
                return completed == stoppedTcs.Task;
            }
            finally
            {
                Server.StatusChanged -= OnStatus;
            }
        }

        /// <summary>
        /// Newest write time (UTC) of this run's world data files, scoped to <see cref="WorldName"/> across
        /// both the legacy (<c>worlds/&lt;World&gt;.db|.fwl</c>) and 1.0+ (<c>worlds_local/&lt;World&gt;/*.db2|*.fwl2</c>)
        /// layouts. This is the filesystem truth a save-flush assertion pins — not the parser's log claim.
        /// Returns null if the world has not been written yet.
        /// </summary>
        public DateTime? LatestWorldSaveTimeUtc()
        {
            try
            {
                var files = WorldDataFiles().ToList();
                return files.Count == 0 ? null : files.Max(f => f.LastWriteTimeUtc);
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<FileInfo> WorldDataFiles()
        {
            static bool IsWorldFile(FileInfo f) => f.Extension is ".db" or ".db2" or ".fwl" or ".fwl2";

            var folderFormat = new DirectoryInfo(Path.Join(SaveDir, "worlds_local", WorldName));
            if (folderFormat.Exists)
            {
                foreach (var f in folderFormat.GetFiles("*", SearchOption.AllDirectories).Where(IsWorldFile))
                {
                    yield return f;
                }
            }

            var legacy = new DirectoryInfo(Path.Join(SaveDir, "worlds"));
            if (legacy.Exists)
            {
                foreach (var f in legacy.GetFiles(WorldName + ".*").Where(IsWorldFile))
                {
                    yield return f;
                }
            }
        }

        public void Dispose()
        {
            // Belt-and-suspenders: never leave a live server behind even if a test bailed mid-boot.
            try
            {
                if (Processes.ServerProcess is { HasExited: false } p) p.Kill(entireProcessTree: true);
            }
            catch { /* already gone */ }

            try { Server.Dispose(); } catch { /* best effort */ }
            _services.Dispose();
        }
    }
}
