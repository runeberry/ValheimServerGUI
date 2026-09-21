using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Game
{
    public class ValheimServer : IDisposable
    {
        /// <summary>
        /// Options for the currently running server.
        /// </summary>
        public IValheimServerOptions Options { get; private set; } = new ValheimServerOptions();

        /// <summary>
        /// Exposed for testing.
        /// </summary>
        public IValheimServerLogger? Logger => ServerLogger;

        public ServerStatus Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status = value;
                // Stamp the real start time on the Running transition (cleared once fully Stopped) so uptime
                // derives from the server's own start, not a per-window capture — switching a window onto an
                // already-running profile then reports the true uptime rather than resetting to zero.
                if (value == ServerStatus.Running) StartedAt = DateTimeOffset.Now;
                else if (value == ServerStatus.Stopped) StartedAt = null;
                StatusChanged?.Invoke(this, value);
            }
        }
        private ServerStatus _status = ServerStatus.Stopped;

        /// <summary>When the server last reached <see cref="ServerStatus.Running"/> (null until then / once Stopped).</summary>
        public DateTimeOffset? StartedAt { get; private set; }

        private string? ProcessKey;
        private bool IsRestarting;

        public event EventHandler<ServerStatus>? StatusChanged;
        public event EventHandler<decimal>? WorldSaved;
        public event EventHandler<string>? InviteCodeReady;

        /// <summary>
        /// Raised when a graceful <see cref="Stop"/> did not complete within <see cref="GracefulStopTimeout"/>
        /// and the process was force-killed (§16.2). The shell surfaces this as a potential-save-loss warning.
        /// </summary>
        public event EventHandler? StopTimedOut;

        /// <summary>
        /// How long <see cref="Stop"/> waits for a graceful shutdown before force-killing the process
        /// (§16.2 enhancement). Set to <see cref="TimeSpan.Zero"/> or less to disable the force-kill fallback.
        /// </summary>
        public TimeSpan GracefulStopTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public bool CanStart => IsAnyStatus(ServerStatus.Stopped) && ProcessKey == null;
        public bool CanStop => IsAnyStatus(ServerStatus.Starting, ServerStatus.Running) && ProcessKey != null;
        public bool CanRestart => IsAnyStatus(ServerStatus.Running) && ProcessKey != null;

        private readonly IProcessProvider ProcessProvider;
        private readonly IApplicationLogger ApplicationLogger;
        private readonly IValheimPathResolver PathResolver;
        private readonly IPlayerAccessListService AccessLists;
        private readonly ServerLogParser Parser;

        /// <summary>
        /// This logger is instantiated each time a new server is started.
        /// </summary>
        private IValheimServerLogger? ServerLogger;

        public ValheimServer(
            IProcessProvider processProvider,
            IPlayerDataRepository playerDataRepository,
            IApplicationLogger appLogger,
            IValheimPathResolver pathResolver,
            IPlayerAccessListService accessLists)
        {
            ProcessProvider = processProvider;
            ApplicationLogger = appLogger;
            PathResolver = pathResolver;
            AccessLists = accessLists;

            // The parser owns the fragile log->event translation and the player correlation; this
            // class keeps the one piece of state the parser deliberately does not: the stop-during-
            // startup guard applied in OnServerConnected.
            Parser = new ServerLogParser(playerDataRepository, appLogger);
            Parser.ServerConnected += OnServerConnected;
            Parser.WorldSaved += (_, timeMs) => WorldSaved?.Invoke(this, timeMs);
            Parser.InviteCodeReady += (_, code) => InviteCodeReady?.Invoke(this, code);

            InitializeStatusBasedActions();
        }

        #region Initialization

        private void InitializeStatusBasedActions()
        {
            StatusChanged += BuildStatusHandler(ServerStatus.Stopped, () =>
            {
                if (IsRestarting)
                {
                    // There are no more server events to listen to after it has stopped, so
                    // we're just going to artifically delay here to allow any shutdown actions to finish
                    Task.Run(async () =>
                    {
                        await Task.Delay(500);

                        if (!IsRestarting) return;

                        IsRestarting = false;
                        Start(Options);
                    });
                }
            });
        }

        private static EventHandler<ServerStatus> BuildStatusHandler(ServerStatus status, Action action)
        {
            return (obj, s) =>
            {
                if (s == status) action();
            };
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Starts the Valheim server as a background process with the provided options.
        /// </summary>
        public void Start(IValheimServerOptions options)
        {
            if (!CanStart) return;

            ApplicationLogger.Information("Starting server: {name}", options.Name);

            var exePath = options.GetValidatedServerExe().FullName;
            var processArgs = GenerateArgs(options);

            ApplicationLogger.Information(
                @"Server run command: ""{exePath}"" {processArgs}",
                exePath,
                CleanArgsForLogging(processArgs));

            GenerateAccessLists(options);

            ProcessKey = Guid.NewGuid().ToString();
            var process = ProcessProvider.AddBackgroundProcess(ProcessKey, exePath, processArgs);

            // Indexer, not Add: the inherited environment may already carry SteamAppId (e.g. launched
            // from a Steam context), and StringDictionary.Add throws on a duplicate key.
            process.StartInfo.EnvironmentVariables["SteamAppId"] = CoreConstants.ValheimSteamAppId;

            // The server binary loads steamclient.so from its own directory (and linux64/ beneath it)
            // and reads steam_appid.txt from the working directory, so both must point at the exe's
            // folder. Windows adds the exe's directory to the DLL search path itself, but on Linux the
            // loader needs LD_LIBRARY_PATH set explicitly or the raw binary fails to start.
            var exeDir = Path.GetDirectoryName(exePath);
            process.StartInfo.WorkingDirectory = exeDir;
            if (!OperatingSystem.IsWindows() && exeDir is not null)
            {
                var envVars = process.StartInfo.EnvironmentVariables;
                // The indexer throws on a missing key in this runtime, so probe before reading.
                var prior = envVars.ContainsKey("LD_LIBRARY_PATH") ? envVars["LD_LIBRARY_PATH"] : null;
                envVars["LD_LIBRARY_PATH"] =
                    string.Join(':', new[] { exeDir, Path.Combine(exeDir, "linux64"), prior }.Where(s => !string.IsNullOrEmpty(s)));
            }

            process.OutputDataReceived += Process_OnDataReceived;
            process.ErrorDataReceived += Process_OnErrorReceived;
            process.Exited += (obj, e) =>
            {
                ProcessKey = null;
                Status = ServerStatus.Stopped;
            };

            ServerLogger = new ValheimServerLogger(options, PathResolver);
            ServerLogger.LogReceived += Logger_OnServerLogReceived;
            if (options.LogMessageHandler != null)
            {
                // Use this to pass messages to a UI component in the MainWindow
                ServerLogger.LogReceived += options.LogMessageHandler;
            }

            ProcessProvider.StartIO(process);

            Options = options;
            IsRestarting = false;
            Status = ServerStatus.Starting;
        }

        /// <summary>
        /// Gracefully stops the Valheim server process.
        /// </summary>
        public void Stop()
        {
            if (!CanStop) return;

            ApplicationLogger.Information("Stopping server: {name}", Options.Name);

            ProcessProvider.SafelyKillProcess(ProcessKey!);

            IsRestarting = false;
            Status = ServerStatus.Stopping;

            ScheduleStopTimeout(ProcessKey);
        }

        // §16.2 enhancement: if the graceful stop leaves the server "Stopping" past the timeout, force-kill
        // the (same) process and warn about potential save loss. The process's Exited handler resets
        // ProcessKey/Status, so a normal shutdown before the timeout is a no-op here.
        private void ScheduleStopTimeout(string? key)
        {
            var timeout = GracefulStopTimeout;
            if (key == null || timeout <= TimeSpan.Zero) return;

            Task.Run(async () =>
            {
                await Task.Delay(timeout);

                if (Status != ServerStatus.Stopping || ProcessKey != key) return;

                ApplicationLogger.Warning(
                    "Server '{name}' did not stop within {timeout}; force-killing (world save may be lost).",
                    Options.Name, timeout);

                ProcessProvider.ForceKillProcess(key);
                StopTimedOut?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Gracefully stops the Valheim server process, then starts it up again.
        /// If no options are provided, then the existing server options will be used.
        /// </summary>
        public void Restart(IValheimServerOptions? options = null)
        {
            if (!CanRestart) return;

            ApplicationLogger.Information("Restarting server: {name}", Options.Name);

            ProcessProvider.SafelyKillProcess(ProcessKey!);

            Options = options ?? Options;
            IsRestarting = true;
            Status = ServerStatus.Stopping;
        }

        public bool IsAnyStatus(params ServerStatus[] statuses)
        {
            return statuses.Any(s => s == Status);
        }

        #endregion

        #region Event handlers

        private void OnServerConnected(object? sender, EventArgs e)
        {
            // The server can reach a running state if you attempt to stop it late in the
            // startup process, so avoid changing status from "Stopping" -> "Running".
            // It will still stop after it fully starts up.
            if (Status == ServerStatus.Stopping) return;

            Status = ServerStatus.Running;
        }

        private void Process_OnDataReceived(object obj, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            ServerLogger?.Information(e.Data);
        }

        private void Process_OnErrorReceived(object obj, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            ServerLogger?.Error(e.Data);
        }

        private void Logger_OnServerLogReceived(string message)
        {
            Parser.ProcessLine(message);
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            Stop();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Helper methods

        // Projects the profile's player roles + usePermittedList flag onto the three gating files immediately
        // before launch, so a manual start, an auto-start, and a restart (all of which re-enter Start) apply
        // the same rules uniformly.
        //
        // DATA-LOSS CAVEAT (accepted for now — see the v3.0 plan): generation OVERWRITES the three files
        // unconditionally, even when the profile has no roles (they become header-only). The first start after
        // this feature shipped therefore wipes any pre-existing manual entries in adminlist/bannedlist/
        // permittedlist.txt. Importing existing files into the role config (and conflict detection) is a
        // deliberately separate pass that MUST land before v3.0 GA.
        private void GenerateAccessLists(IValheimServerOptions options)
        {
            var saveDataFolder = options.GetValidatedSaveDataFolder().FullName;

            var roles = options.PlayerRoles.Select(a => (
                new PlayerInfo { Platform = a.Platform, PlatformRaw = a.PlatformRaw, PlayerId = a.PlayerId },
                a.Role));

            AccessLists.GenerateFiles(saveDataFolder, roles, options.UsePermittedList);

            ApplicationLogger.Information(
                "Generated access lists in {folder}: {count} role(s), usePermittedList={mode}",
                saveDataFolder, options.PlayerRoles.Count, options.UsePermittedList);
        }

        private static string GenerateArgs(IValheimServerOptions options)
        {
            var saveDataFolder = options.GetValidatedSaveDataFolder().FullName;
            var publicFlag = options.Public ? 1 : 0;
            var processArgs = @$"-nographics -batchmode -name ""{options.Name}"" -port {options.Port} -world ""{options.WorldName}"" -public {publicFlag} -savedir ""{saveDataFolder}"" -saveinterval {options.SaveInterval} -backups {options.Backups} -backupshort {options.BackupShort} -backuplong {options.BackupLong}";

            if (!string.IsNullOrWhiteSpace(options.Password))
            {
                processArgs += @$" -password ""{options.Password}""";
            }

            if (options.Crossplay)
            {
                processArgs += " -crossplay";
            }

            if (!string.IsNullOrWhiteSpace(options.WorldPreset))
            {
                processArgs += $" -preset {options.WorldPreset}";
            }
            else if (options.WorldModifiers != null)
            {
                foreach (var (key, value) in options.WorldModifiers)
                {
                    processArgs += $" -modifier {key} {value}";
                }
            }

            if (options.WorldKeys != null)
            {
                foreach (var key in options.WorldKeys)
                {
                    processArgs += $" -setkey {key}";
                }
            }

            if (!string.IsNullOrWhiteSpace(options.AdditionalArgs))
            {
                processArgs += $" {options.AdditionalArgs}";
            }

            return processArgs;
        }

        private static string CleanArgsForLogging(string processArgs)
        {
            // Don't print server password to logs
            processArgs = Regex.Replace(processArgs, @"-password ""(.*?)""", @"-password ""*****""");

            return processArgs;
        }

        #endregion
    }
}
