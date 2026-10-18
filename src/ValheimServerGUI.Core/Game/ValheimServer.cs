using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        /// Raised when changes made to the list files during play (the game's in-game ban/permit commands, or a hand
        /// edit) were adopted into this server's role overrides before a restart or live role change rewrote them.
        /// The owner persists <see cref="ReconcileResult.Overrides"/> to the profile. May be raised off the UI thread.
        /// </summary>
        public event EventHandler<ReconcileResult>? PlayerRolesAdopted;

        /// <summary>The variable the server reads its Steam app id from; always set by <see cref="Start"/>.</summary>
        public const string SteamAppIdVariable = "SteamAppId";

        /// <summary>The variables <see cref="Start"/> sets when it enables BepInEx's loader (Linux).</summary>
        public static readonly IReadOnlySet<string> BepInExLoaderVariables =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DOORSTOP_ENABLED", "DOORSTOP_TARGET_ASSEMBLY" };

        public bool CanStart => IsAnyStatus(ServerStatus.Stopped) && ProcessKey == null;
        public bool CanStop => IsAnyStatus(ServerStatus.Starting, ServerStatus.Running) && ProcessKey != null;
        public bool CanRestart => IsAnyStatus(ServerStatus.Running) && ProcessKey != null;

        private readonly IProcessProvider ProcessProvider;
        private readonly IPlayerDataRepository PlayerDataRepository;
        private readonly IApplicationLogger ApplicationLogger;
        private readonly IValheimPathResolver PathResolver;
        private readonly IPlayerAccessListService AccessLists;
        private readonly IPlayerListImportService ListImport;
        private readonly IPlayerListBaselineStore ListBaselines;
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
            IPlayerAccessListService accessLists,
            IPlayerListImportService listImport,
            IPlayerListBaselineStore listBaselines)
        {
            ProcessProvider = processProvider;
            PlayerDataRepository = playerDataRepository;
            ApplicationLogger = appLogger;
            PathResolver = pathResolver;
            AccessLists = accessLists;
            ListImport = listImport;
            ListBaselines = listBaselines;

            // The parser owns the fragile log->event translation and the player correlation; this
            // class keeps the one piece of state the parser deliberately does not: the stop-during-
            // startup guard applied in OnServerConnected.
            Parser = new ServerLogParser(playerDataRepository, appLogger);
            Parser.ServerConnected += OnServerConnected;
            Parser.WorldSaved += OnWorldSaved;
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
                        try
                        {
                            // A restart is still "during play": adopt list changes made since the last write.
                            StartCore(Options, adoptExternalChanges: true);
                        }
                        catch (Exception ex)
                        {
                            ApplicationLogger.Error(ex, "Failed to restart server: {message}", ex.Message);
                        }
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
        public void Start(IValheimServerOptions options) => StartCore(options, adoptExternalChanges: false);

        private void StartCore(IValheimServerOptions options, bool adoptExternalChanges)
        {
            if (!CanStart) return;

            ApplicationLogger.Information("Starting server: {name}", options.Name);

            var exePath = options.GetValidatedServerExe().FullName;
            var processArgs = GenerateArgs(options);
            // Parsed before anything launches, so malformed input stops the start here.
            var profileEnvironment = EnvironmentVariableParser.Parse(options.EnvironmentVariables);

            // The profile's variables are logged as typed, ahead of the command, the way a shell would run it.
            var envPrefix = string.IsNullOrWhiteSpace(options.EnvironmentVariables) ? string.Empty : options.EnvironmentVariables.Trim() + " ";
            ApplicationLogger.Information(
                @"Server run command: {envPrefix:l}""{exePath}"" {processArgs}",
                envPrefix,
                exePath,
                CleanArgsForLogging(processArgs));

            // Before anything launches: a failed list backup throws here and the server is not started.
            SyncAccessLists(options, adoptExternalChanges);
            RefreshPlayerNamesFromWorldSave(options);

            ProcessKey = Guid.NewGuid().ToString();
            var process = ProcessProvider.AddBackgroundProcess(ProcessKey, exePath, processArgs);

            // Layering: inherited environment, then the profile's variables (replacing inherited ones of the same
            // name), then what the launch itself requires. Validation rejects profile variables that the last
            // layer would overwrite; the path lists below keep the profile's value and prepend to it.
            // Indexer, not Add: StringDictionary.Add throws on a key the inherited environment already has.
            var envVars = process.StartInfo.EnvironmentVariables;
            foreach (var (name, value) in profileEnvironment) envVars[name] = value;
            envVars[SteamAppIdVariable] = CoreConstants.ValheimSteamAppId;

            // The server binary loads steamclient.so from its own directory (and linux64/ beneath it)
            // and reads steam_appid.txt from the working directory, so both must point at the exe's
            // folder. Windows adds the exe's directory to the DLL search path itself, but on Linux the
            // loader needs LD_LIBRARY_PATH set explicitly or the raw binary fails to start.
            var exeDir = Path.GetDirectoryName(exePath);
            process.StartInfo.WorkingDirectory = exeDir;
            if (!OperatingSystem.IsWindows() && exeDir is not null)
            {
                // The indexer throws on a missing key in this runtime, so probe before reading.
                var prior = envVars.ContainsKey("LD_LIBRARY_PATH") ? envVars["LD_LIBRARY_PATH"] : null;
                envVars["LD_LIBRARY_PATH"] =
                    string.Join(':', new[] { exeDir, Path.Combine(exeDir, "linux64"), prior }.Where(s => !string.IsNullOrEmpty(s)));

                EnableBepInExLoader(envVars, options);
            }

            // Windows: BepInEx's console output never reaches stdout there, so its disk log is fed in instead.
            var bepInExLog = OperatingSystem.IsWindows() && exeDir is not null
                ? new BepInExLogTail(Path.Join(exeDir, "BepInEx", "LogOutput.log"), DateTime.UtcNow, line => ServerLogger?.Information(line))
                : null;

            process.OutputDataReceived += Process_OnDataReceived;
            process.ErrorDataReceived += Process_OnErrorReceived;
            process.Exited += (obj, e) =>
            {
                bepInExLog?.Complete();
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
            bepInExLog?.Start(TimeSpan.FromMilliseconds(500));

            Options = options;
            IsRestarting = false;
            Status = ServerStatus.Starting;
        }

        /// <summary>
        /// Gracefully stops the Valheim server process. The server stays <see cref="ServerStatus.Stopping"/> until the
        /// process exits, however long its shutdown takes (it saves the world first); it is never force-killed.
        /// </summary>
        public void Stop()
        {
            if (!CanStop) return;

            ApplicationLogger.Information("Stopping server: {name}", Options.Name);

            ProcessProvider.SafelyKillProcess(ProcessKey!);

            IsRestarting = false;
            Status = ServerStatus.Stopping;
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

        private void OnWorldSaved(object? sender, decimal timeMs)
        {
            // The completion line is logged after the save is committed, so the new history is on disk.
            RefreshPlayerNamesFromWorldSave(Options);
            WorldSaved?.Invoke(this, timeMs);
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

        // Writes the profile's effective player roles + usePermittedList flag to the three gating files. Every write
        // path goes through here — start, restart, and live role changes — so the list-file safety rules hold for all
        // of them: the writer never drops an entry VSG didn't write without backing the file up first (and writes
        // nothing if the backup fails), and with adoptExternalChanges the changes made during play (the game's
        // in-game ban/permit commands, or hand edits) are adopted into the overrides first and win. A manual start
        // passes false: MainWindowViewModel has already reconciled the files, asking the user about any conflict.
        private void SyncAccessLists(IValheimServerOptions options, bool adoptExternalChanges)
        {
            var saveDataFolder = options.GetValidatedSaveDataFolder().FullName;
            var baseline = ListBaselines.Get(saveDataFolder);

            if (adoptExternalChanges && options is ValheimServerOptions mutable)
            {
                var adopted = ListImport.Reconcile(
                    saveDataFolder, mutable.RoleOverrides, mutable.RoleDefaults, externalWins: true);
                foreach (var line in adopted.LogLines) ApplicationLogger.Information("{line}", line);
                if (adopted.Changed)
                {
                    mutable.RoleOverrides = adopted.Overrides;
                    PlayerRolesAdopted?.Invoke(this, adopted);
                }
            }

            var players = options.PlayerRoles.Select(a => (
                new PlayerInfo { Platform = a.Platform, PlatformRaw = a.PlatformRaw, PlayerId = a.PlayerId },
                a.Role));
            var result = AccessLists.WriteLists(saveDataFolder, players, options.UsePermittedList, baseline);
            ListBaselines.Save(saveDataFolder, result.Written);

            foreach (var backup in result.Backups)
                ApplicationLogger.Warning("Backed up {file} before rewriting it (it had entries VSG did not write).", backup.FullName);
            ApplicationLogger.Information(
                "Wrote access lists in {folder}: {count} role(s), usePermittedList={mode}",
                saveDataFolder, options.PlayerRoles.Count, options.UsePermittedList);
        }

        /// <summary>
        /// Applies a player-role change to the <b>already-running</b> server (the carve-out from the
        /// "changes only while stopped" rule): updates the live <see cref="Options"/> so a later restart keeps
        /// the change, then rewrites the three list files, which the server re-reads within seconds. Changes made
        /// to the files during play are adopted first (see <see cref="PlayerRolesAdopted"/>). Only the roles + flag
        /// change — every other launch option is left exactly as it was at start.
        /// </summary>
        public void ApplyPlayerRoles(
            IReadOnlyDictionary<string, PlayerRoleEntry> overrides,
            IReadOnlyDictionary<string, PlayerDefaultEntry> defaults,
            bool usePermittedList)
        {
            // Keep the live options in step: Restart reuses Options, so without this a restart would rewrite from
            // the stale start-time roles and revert the change.
            if (Options is ValheimServerOptions options)
            {
                options.RoleOverrides = overrides;
                options.RoleDefaults = defaults;
                options.UsePermittedList = usePermittedList;
            }

            SyncAccessLists(Options, adoptExternalChanges: true);
        }

        // The world save records every player who has joined, with the platform name each client reported for itself
        // (Steam persona, Xbox gamertag, ...). Reading it at start and after each save names known players without
        // an API lookup. A missing or unreadable save is logged and never blocks the server.
        private void RefreshPlayerNamesFromWorldSave(IValheimServerOptions options)
        {
            try
            {
                var worldFolder = options.GetValidatedSaveDataFolder().GetLocalWorldFolder(options.WorldName ?? string.Empty);
                PlayerDataRepository.ApplyWorldPlayerHistory(WorldPlayerHistory.ReadFromWorldFolder(worldFolder));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
            {
                ApplicationLogger.Warning(e, "Could not read player names from the save of world {world}: {message}",
                    options.WorldName, e.Message);
            }
        }

        /// <summary>
        /// BepInEx loads through Unity Doorstop. On Windows, Doorstop is a winhttp.dll proxy in the server folder that
        /// the game loads by itself. On Linux it is a preload library that only loads when the launch environment
        /// names it (the BepInEx pack's start_server_bepinex.sh sets these same variables), so without them a modded
        /// server starts unmodded with no sign of why. The two files come from the configured paths (Set
        /// Directories); when either is missing the server starts without mods and the log says which.
        /// </summary>
        private void EnableBepInExLoader(StringDictionary envVars, IValheimServerOptions options)
        {
            if (!options.LoadsBepInEx())
            {
                var missing = File.Exists(options.BepInExPreloaderPath) ? options.DoorstopLibraryPath : options.BepInExPreloaderPath;
                ApplicationLogger.Information("BepInEx not loaded, starting without mods: {path} not found", missing);
                return;
            }

            ApplicationLogger.Information("Loading BepInEx: {preloader} via {doorstop}", options.BepInExPreloaderPath, options.DoorstopLibraryPath);

            envVars["DOORSTOP_ENABLED"] = "1";
            envVars["DOORSTOP_TARGET_ASSEMBLY"] = options.BepInExPreloaderPath;

            // ld.so splits LD_PRELOAD on spaces as well as colons, with no escaping, so a full path through Steam's
            // "Valheim dedicated server" folder would never load. LD_LIBRARY_PATH splits on colons only, so the
            // library's folder goes there and LD_PRELOAD names just the file, which ld.so resolves through that path.
            // The BepInEx pack's start_server_bepinex.sh does the same.
            // The indexer throws on a missing key in this runtime, so probe before reading.
            var doorstopDir = Path.GetDirectoryName(options.DoorstopLibraryPath);
            var priorLibraryPath = envVars.ContainsKey("LD_LIBRARY_PATH") ? envVars["LD_LIBRARY_PATH"] : null;
            envVars["LD_LIBRARY_PATH"] = string.Join(':', new[] { doorstopDir, priorLibraryPath }.Where(s => !string.IsNullOrEmpty(s)));

            var priorPreload = envVars.ContainsKey("LD_PRELOAD") ? envVars["LD_PRELOAD"] : null;
            envVars["LD_PRELOAD"] = string.Join(':', new[] { Path.GetFileName(options.DoorstopLibraryPath), priorPreload }.Where(s => !string.IsNullOrEmpty(s)));
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
