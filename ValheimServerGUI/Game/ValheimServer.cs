using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
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
        public ValheimServerLogger Logger => this.ServerLogger;

        public ServerStatus Status
        {
            get => this._status;
            private set
            {
                if (this._status == value) return;
                this._status = value;
                this.StatusChanged?.Invoke(this, value);
            }
        }
        private ServerStatus _status = ServerStatus.Stopped;
        private string ProcessKey;
        private bool IsRestarting;
        private readonly Dictionary<string, LogEventHandler> LogBasedActions = new();

        public event EventHandler<ServerStatus> StatusChanged;
        public event EventHandler<decimal> WorldSaved;
        public event EventHandler<string> InviteCodeReady;
        public event EventHandler<EventLogContext> LogReceived;

        public bool CanStart => this.IsAnyStatus(ServerStatus.Stopped) && this.ProcessKey == null;
        public bool CanStop => this.IsAnyStatus(ServerStatus.Starting, ServerStatus.Running) && this.ProcessKey != null;
        public bool CanRestart => this.IsAnyStatus(ServerStatus.Running) && this.ProcessKey != null;

        private readonly IProcessProvider ProcessProvider;
        private readonly IValheimFileProvider FileProvider;
        private readonly IPlayerDataRepository PlayerDataRepository;
        private readonly ValheimServerLogger ServerLogger;
        private readonly IEventLogger ApplicationLogger;

        public ValheimServer(
            IProcessProvider processProvider,
            IValheimFileProvider fileProvider,
            IPlayerDataRepository playerDataRepository,
            ValheimServerLogger serverLogger,
            IEventLogger appLogger)
        {
            this.ProcessProvider = processProvider;
            this.FileProvider = fileProvider;
            this.PlayerDataRepository = playerDataRepository;
            this.ServerLogger = serverLogger;
            this.ApplicationLogger = appLogger;

            this.ServerLogger.LogReceived += this.Logger_OnServerLogReceived;

            InitializeLogBasedActions();
            InitializeStatusBasedActions();
        }

        #region Initialization

        private void InitializeLogBasedActions()
        {

            LogBasedActions.Add(@"Game server connected", this.OnServerConnected);
            LogBasedActions.Add(@"World saved \(\s*?([[\d\.]+?)\s*?ms\s*?\)\s*?$", this.OnWorldSaved);
            LogBasedActions.Add(@"Session "".*?"" with join code (.*?) ", this.OnCrossplayJoinCodeAvailable);

            // Connecting
            LogBasedActions.Add(@"Got connection SteamID (\d+?)\D*?$", this.OnPlayerConnecting);
            LogBasedActions.Add(@"PlayFab socket with remote ID .*? received local Platform ID Steam_(\d+?)$", this.OnPlayerConnecting); // Crossplay

            // Connected - NOTE: ZDOID can be a negative number, account for that w/ regex!
            LogBasedActions.Add(@"Got character ZDOID from (.+?) : ([\d-]+?)\D*?:(\d+?)\D*?$", this.OnPlayerConnected);

            // Disconnecting
            LogBasedActions.Add(@"Peer (\d+?) has wrong password", this.OnPlayerDisconnecting);

            // Disconnected
            LogBasedActions.Add(@"Closing socket (\d+?)\D*?$", this.OnPlayerDisconnected); // This is technically "disconnecting" but it's the best terminator I can find
            LogBasedActions.Add(@"Destroying abandoned non persistent zdo ([\d-]+?):.*$", this.OnPlayerDisconnected); // Crossplay
        }

        private void InitializeStatusBasedActions()
        {
            this.StatusChanged += BuildStatusHandler(ServerStatus.Stopped, () =>
            {
                if (this.IsRestarting)
                {
                    // There are no more server events to listen to after it has stopped, so
                    // we're just going to artifically delay here to allow any shutdown actions to finish
                    Task.Run(async () =>
                    {
                        await Task.Delay(500);

                        if (!this.IsRestarting) return;

                        this.IsRestarting = false;
                        this.Start(this.Options);
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
            if (!this.CanStart) return;

            this.ApplicationLogger.LogInformation("Starting server: {name}", options.Name);

            var exePath = this.FileProvider.ServerExe.FullName;
            var processArgs = this.GenerateArgs(options);
            this.ApplicationLogger.LogInformation(@"Server run command: ""{exePath}"" {processArgs}", exePath, processArgs);

            this.ProcessKey = Guid.NewGuid().ToString();
            var process = this.ProcessProvider.AddBackgroundProcess(this.ProcessKey, exePath, processArgs);

            process.StartInfo.EnvironmentVariables.Add("SteamAppId", Resources.ValheimSteamAppId);
            process.OutputDataReceived += this.Process_OnDataReceived;
            process.ErrorDataReceived += this.Process_OnErrorReceived;
            process.Exited += (obj, e) =>
            {
                this.ProcessKey = null;
                this.Status = ServerStatus.Stopped;
            };

            process.StartIO();

            this.Options = options;
            this.IsRestarting = false;
            this.Status = ServerStatus.Starting;
        }

        /// <summary>
        /// Gracefully stops the Valheim server process.
        /// </summary>
        public void Stop()
        {
            if (!this.CanStop) return;

            this.ApplicationLogger.LogInformation("Stopping server: {name}", this.Options.Name);

            this.ProcessProvider.SafelyKillProcess(this.ProcessKey);

            this.IsRestarting = false;
            this.Status = ServerStatus.Stopping;
        }

        /// <summary>
        /// Gracefully stops the Valheim server process, then starts it up again.
        /// If no options are provided, then the existing server options will be used.
        /// </summary>
        public void Restart(IValheimServerOptions options = null)
        {
            if (!this.CanRestart) return;

            this.ApplicationLogger.LogInformation("Restarting server: {name}", this.Options.Name);

            this.ProcessProvider.SafelyKillProcess(this.ProcessKey);

            this.Options = options ?? this.Options;
            this.IsRestarting = true;
            this.Status = ServerStatus.Stopping;
        }

        public bool IsAnyStatus(params ServerStatus[] statuses)
        {
            return statuses.Any(s => s == this.Status);
        }

        #endregion

        #region Event handlers

        private void Process_OnDataReceived(object obj, DataReceivedEventArgs e)
        {
            ServerLogger.LogInformation(e.Data);
        }

        private void Process_OnErrorReceived(object obj, DataReceivedEventArgs e)
        {
            ServerLogger.LogError(e.Data);
        }

        private void Logger_OnServerLogReceived(object obj, EventLogContext context)
        {
            foreach (var kvp in this.LogBasedActions)
            {
                var match = Regex.Match(context.Message, kvp.Key, RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                try
                {
                    // The first capture group is the whole string, so skip that
                    var captures = (match.Groups as IEnumerable<Group>).Skip(1).Select(g => g.ToString()).ToArray();
                    kvp.Value(this, context, captures);
                }
                catch (Exception e)
                {
                    this.ApplicationLogger.LogError(e, "Error parsing server log: {message}", context.Message);
                }
            }

            this.LogReceived?.Invoke(obj, context);
        }

        #endregion

        #region Log Message handlers

        private void OnServerConnected(object sender, EventLogContext context, params string[] captures)
        {
            this.Status = ServerStatus.Running;
        }

        private void OnPlayerConnecting(object sender, EventLogContext context, params string[] captures)
        {
            var steamId = captures[0];
            if (string.IsNullOrWhiteSpace(steamId)) return;

            this.PlayerDataRepository.SetPlayerJoining(steamId);
        }

        private void OnPlayerConnected(object sender, EventLogContext context, params string[] captures)
        {
            var playerName = captures[0];
            var zdoid = captures[1]; // Seems to be a unique object id for the game session
            //var otherNumber = captures[2]; // Not sure what this is for?

            if (string.IsNullOrWhiteSpace(playerName)) return;

            this.PlayerDataRepository.SetPlayerOnline(playerName, zdoid);
        }

        private void OnPlayerDisconnecting(object sender, EventLogContext context, params string[] captures)
        {
            var steamOrZdoId = captures[0];
            if (string.IsNullOrWhiteSpace(steamOrZdoId)) return;

            this.PlayerDataRepository.SetPlayerLeaving(steamOrZdoId);
        }

        private void OnPlayerDisconnected(object sender, EventLogContext context, params string[] captures)
        {
            var steamOrZdoId = captures[0];
            if (string.IsNullOrWhiteSpace(steamOrZdoId)) return;

            this.PlayerDataRepository.SetPlayerOffline(steamOrZdoId);
        }

        private void OnWorldSaved(object sender, EventLogContext context, params string[] captures)
        {
            if (!decimal.TryParse(captures[0], out var timeMs))
            {
                timeMs = 0;
            }

            this.WorldSaved?.Invoke(this, timeMs);
        }

        private void OnCrossplayJoinCodeAvailable(object sender, EventLogContext context, params string[] captures)
        {
            var inviteCode = captures[0];
            if (string.IsNullOrWhiteSpace(inviteCode)) return;

            this.InviteCodeReady?.Invoke(this, inviteCode);
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            this.Stop();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Helper methods

        private string GenerateArgs(IValheimServerOptions options)
        {
            var saveDataFolder = this.FileProvider.SaveDataFolder.FullName;
            var publicFlag = options.Public ? 1 : 0;
            var processArgs = @$"-nographics -batchmode -name ""{options.Name}"" -port {options.Port} -world ""{options.WorldName}"" -password ""{options.Password}"" -public {publicFlag} -savedir ""{saveDataFolder}"" -saveinterval {options.SaveInterval} -backups {options.Backups} -backupshort {options.BackupShort} -backuplong {options.BackupLong}";

            if (options.Crossplay)
            {
                processArgs += " -crossplay";
            }

            if (!string.IsNullOrWhiteSpace(options.AdditionalArgs))
            {
                processArgs += options.AdditionalArgs;
            }

            // TODO: can't actually enable this right now, because it stops the process from writing out logs,
            // which are essential for the app to function. Need to implement my own file logging, or find a way
            // to read logs from file as they come in.
            //if (!string.IsNullOrWhiteSpace(options.LogFile))
            //{
            //    processArgs += @$" -logFile ""{options.LogFile}""";
            //}

            return processArgs;
        }

        #endregion
    }
}
