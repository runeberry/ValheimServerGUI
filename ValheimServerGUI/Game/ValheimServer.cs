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
        public event EventHandler<ServerStatus> StatusChanged;

        public event EventHandler<decimal> WorldSaved;

        public bool CanStart => this.IsAnyStatus(ServerStatus.Stopped);
        public bool CanStop => this.IsAnyStatus(ServerStatus.Starting, ServerStatus.Running);
        public bool CanRestart => this.IsAnyStatus(ServerStatus.Running);

        private bool IsRestarting;
        private readonly Dictionary<string, LogEventHandler> LogBasedActions = new();

        private readonly IProcessProvider ProcessProvider;
        private readonly IValheimFileProvider FileProvider;
        private readonly IPlayerDataProvider PlayerDataProvider;
        private readonly ValheimServerLogger ServerLogger;
        private readonly ApplicationLogger ApplicationLogger;
        
        public ValheimServer(
            IProcessProvider processProvider, 
            IValheimFileProvider fileProvider,
            IPlayerDataProvider playerDataProvider,
            ValheimServerLogger serverLogger,
            ApplicationLogger appLogger)
        {
            this.ProcessProvider = processProvider;
            this.FileProvider = fileProvider;
            this.PlayerDataProvider = playerDataProvider;
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
            
            LogBasedActions.Add(@"Got connection SteamID (\d+?)\D*?$", this.OnPlayerConnecting);
            LogBasedActions.Add(@"Got character ZDOID from (\S+?)\s*?:\s*?([\d-]+?)\D*?:(\d+?)\D*?$", this.OnPlayerConnected);
            LogBasedActions.Add(@"Peer (\d+?) has wrong password", this.OnPlayerDisconnecting);
            LogBasedActions.Add(@"Closing socket (\d+?)\D*?$", this.OnPlayerDisconnected); // Technically "disconnecting" but it's the best terminator I can find
            
            LogBasedActions.Add(@"World saved \(\s*?([[\d\.]+?)\s*?ms\s*?\)\s*?$", this.OnWorldSaved);
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

            var exePath = this.FileProvider.ServerExe.FullName;
            var publicFlag = options.Public ? 1 : 0;
            var processArgs = @$"-nographics -batchmode -name ""{options.Name}"" -port {options.Port} -world ""{options.WorldName}"" -password ""{options.Password}"" -public {publicFlag}";
            var process = this.ProcessProvider.AddBackgroundProcess(ProcessKeys.ValheimServer, exePath, processArgs);

            process.StartInfo.EnvironmentVariables.Add("SteamAppId", Resources.ValheimSteamAppId);
            process.OutputDataReceived += this.Process_OnDataReceived;
            process.ErrorDataReceived += this.Process_OnErrorReceived;
            process.Exited += (obj, e) => this.Status = ServerStatus.Stopped;

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

            this.ProcessProvider.SafelyKillProcess(ProcessKeys.ValheimServer);

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

            this.ProcessProvider.SafelyKillProcess(ProcessKeys.ValheimServer);

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
                    this.ApplicationLogger.LogError(e, "Error parsing server log: {0}", context.Message);
                }
            }
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

            var player = this.FindPlayerBySteamId(steamId, true);
            if (player != null)
            {
                player.PlayerStatus = PlayerStatus.Joining;
                player.LastStatusChange = DateTime.UtcNow;
                player.ZdoId = null;
                this.PlayerDataProvider.Upsert(player);
            }
        }

        private void OnPlayerConnected(object sender, EventLogContext context, params string[] captures)
        {
            var playerName = captures[0];
            var zdoid = captures[1]; // Seems to be a unique object id for the game session
            //var otherNumber = captures[2]; // Not sure what this is for?

            if (string.IsNullOrWhiteSpace(playerName)) return;

            var player = FindJoiningPlayerByName(playerName);
            if (player != null)
            {
                player.PlayerName = playerName;
                player.PlayerStatus = PlayerStatus.Online;
                player.LastStatusChange = DateTime.UtcNow;
                player.ZdoId = zdoid;
                this.PlayerDataProvider.Upsert(player);
            }
        }

        private void OnPlayerDisconnecting(object sender, EventLogContext context, params string[] captures)
        {
            var steamId = captures[0];
            if (string.IsNullOrWhiteSpace(steamId)) return;

            var player = this.FindPlayerBySteamId(steamId);
            if (player != null)
            {
                player.PlayerStatus = PlayerStatus.Leaving;
                player.LastStatusChange = DateTime.UtcNow;
                player.ZdoId = null;
                this.PlayerDataProvider.Upsert(player);
            }
        }

        private void OnPlayerDisconnected(object sender, EventLogContext context, params string[] captures)
        {
            var steamId = captures[0];
            if (string.IsNullOrWhiteSpace(steamId)) return;

            var player = this.FindPlayerBySteamId(steamId);
            if (player != null)
            {
                player.PlayerStatus = PlayerStatus.Offline;
                player.LastStatusChange = DateTime.UtcNow;
                player.ZdoId = null;
                this.PlayerDataProvider.Upsert(player);
            }
        }

        private void OnWorldSaved(object sender, EventLogContext context, params string[] captures)
        {
            if (!decimal.TryParse(captures[0], out var timeMs))
            {
                timeMs = 0;
            }

            this.WorldSaved?.Invoke(this, timeMs);

            // todo: something
        }

        #endregion

        #region Non-public methods

        private PlayerInfo FindPlayerBySteamId(string steamId, bool createNew = false)
        {
            // Gonna sneak a little validation in here don't tell nobody :3
            if (string.IsNullOrWhiteSpace(steamId)) return null;

            var player = this.PlayerDataProvider.FindById(steamId);

            if (player == null && createNew)
            {
                player = new PlayerInfo
                {
                    SteamId = steamId,
                    PlayerStatus = PlayerStatus.Offline, // Considered offline until otherwise defined
                };
                this.PlayerDataProvider.Upsert(player);
            }

            return player;
        }

        private PlayerInfo FindJoiningPlayerByName(string playerName)
        {
            PlayerInfo player;

            // This is tricky becase we don't have the SteamID in this particular log message
            // So consider all players that are currently connecting to the game
            var joiningPlayers = this.PlayerDataProvider.Data.Where(p => p.PlayerStatus == PlayerStatus.Joining);
            var joiningWithSameName = joiningPlayers.Where(p => p.PlayerName == playerName);

            if (joiningPlayers.Count() > 1 && joiningWithSameName.Any())
            {
                // If multiple players are joining, we can narrow down the list if any of the joining
                // players already have a name that matches the requested player name
                joiningPlayers = joiningWithSameName;

                this.ApplicationLogger.LogTrace($"Multiple players joining, but found {joiningPlayers.Count()} match(es) by name {playerName}.");
            }

            if (joiningPlayers.Count() == 1)
            {
                // If there's only one player connecting (or multiple players, but one has the same name), use that player.
                // It's gotta be the same person... right?
                player = joiningPlayers.Single();
                player.SteamIdConfident = true;
                player.SteamIdAlternates = null;

                this.ApplicationLogger.LogTrace($"Matched PlayerName {playerName} to SteamId {player.SteamId} with high confidence.");
            }
            else
            {
                if (this.PlayerDataProvider.ResolveUncertainSteamIds())
                {
                    // Run this method recursively until no more uncertain id updates are made
                    player = this.FindJoiningPlayerByName(playerName);
                }
                else
                {
                    // We tried, but we couldn't resolve this name to a single player.
                    // Simply assume it's the player who's been trying to join the longest.
                    player = joiningPlayers.OrderBy(p => p.LastStatusChange).First();

                    this.ApplicationLogger.LogWarning(
                        $"Multiple players joining, matched PlayerName {playerName} to SteamId {player.SteamId} with low confidence.");
                }
            }
            
            return player;
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            this.Stop();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
