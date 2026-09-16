using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Pure line-to-event translator for Valheim dedicated-server stdout. Extracted from
    /// <c>ValheimServer</c> so the fragile regex table and player-correlation dispatch can be tested
    /// headlessly (feed <see cref="ProcessLine"/> lines, assert against a fake player repository) with
    /// no process, exe, or filesystem. The parser is state-free: it raises <see cref="ServerConnected"/>,
    /// <see cref="WorldSaved"/>, and <see cref="InviteCodeReady"/> and mutates the player repository, but
    /// holds no server status itself. The "don't promote Stopping-&gt;Running" guard lives in
    /// <c>ValheimServer</c>, which subscribes to <see cref="ServerConnected"/>.
    /// </summary>
    public class ServerLogParser
    {
        private delegate void LogEventHandler(string[] captures);

        private readonly Dictionary<string, LogEventHandler> LogBasedActions = new();
        private readonly IPlayerDataRepository PlayerDataRepository;
        private readonly IApplicationLogger ApplicationLogger;

        /// <summary>Raised when the server reports it has connected (reached a running state).</summary>
        public event EventHandler? ServerConnected;

        /// <summary>Raised when the server reports a world save, carrying the save duration in ms.</summary>
        public event EventHandler<decimal>? WorldSaved;

        /// <summary>Raised when the crossplay join (invite) code becomes available.</summary>
        public event EventHandler<string>? InviteCodeReady;

        public ServerLogParser(IPlayerDataRepository playerDataRepository, IApplicationLogger appLogger)
        {
            PlayerDataRepository = playerDataRepository;
            ApplicationLogger = appLogger;

            InitializeLogBasedActions();
        }

        private void InitializeLogBasedActions()
        {
            LogBasedActions.Add(@"Game server connected", OnServerConnected);
            // Legacy world-save line (pre-1.0 dedicated server): "World saved ( 20ms )".
            LogBasedActions.Add(@"World saved \(\s*?([[\d\.]+?)\s*?ms\s*?\)\s*?$", OnWorldSaved);
            // Valheim 1.0+ logs a 5-step save sequence instead; the "World saved ( Nms )" line is gone.
            // Match the completion line and pull the total time from its "[<n>ms]" (tier-4 live smoke, E9).
            LogBasedActions.Add(@"World save \(\d+/\d+\) done\. Total time \[([\d.]+)ms\]", OnWorldSaved);
            LogBasedActions.Add(@"Session "".*?"" with join code (.*?) ", OnCrossplayJoinCodeAvailable);

            // Connecting
            LogBasedActions.Add(@"Got connection SteamID (\d+?)\D*?$", OnPlayerConnecting);
            LogBasedActions.Add(@"PlayFab socket with remote ID .*? received local Platform ID (\w+?)_(\d+?)$", OnPlayerConnectingCrossplay); // Crossplay

            // Connected - NOTE: ZDOID can be a negative number, account for that w/ regex!
            LogBasedActions.Add(@"Got character ZDOID from (.+?) : ([\d-]+?)\D*?:(\d+?)\D*?$", OnPlayerConnected);

            // Disconnecting
            LogBasedActions.Add(@"Peer (\d+?) has wrong password", OnPlayerDisconnecting);

            // Disconnected
            LogBasedActions.Add(@"Closing socket (\d+?)\D*?$", OnPlayerDisconnected); // This is technically "disconnecting" but it's the best terminator I can find
            LogBasedActions.Add(@"Destroying abandoned non persistent zdo ([\d-]+?):.*$", OnPlayerDisconnected); // Crossplay
            LogBasedActions.Add(@"Disconnect: The client \((\w+?)_(\d+?)\)", OnPlayerDisconnectedCrossplay); // Valheim Plus version mismatch
        }

        /// <summary>Feeds one server log line through the regex table, dispatching any matches.</summary>
        public void ProcessLine(string message)
        {
            foreach (var kvp in LogBasedActions)
            {
                var match = Regex.Match(message, kvp.Key, RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                try
                {
                    // The first capture group is the whole string, so skip that
                    var captures = (match.Groups as IEnumerable<Group>).Skip(1).Select(g => g.ToString()).ToArray();
                    kvp.Value(captures);
                }
                catch (Exception e)
                {
                    ApplicationLogger.Error(e, "Error parsing server log: {message}", message);
                }
            }
        }

        #region Log Message handlers

        private void OnServerConnected(params string[] captures)
        {
            // State-free: ValheimServer applies the "don't promote Stopping->Running" guard.
            ServerConnected?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayerConnecting(params string[] captures)
        {
            var steamId = captures[0];
            if (string.IsNullOrWhiteSpace(steamId)) return;

            PlayerDataRepository.SetPlayerJoining(new() { Platform = PlayerPlatforms.Steam, PlatformRaw = PlayerPlatforms.Steam, PlayerId = steamId });
        }

        private void OnPlayerConnectingCrossplay(params string[] captures)
        {
            var rawPlatform = captures[0];
            var hasValidPlatform = PlayerPlatforms.TryGetValidPlatform(rawPlatform, out var platform);
            var playerId = captures[1];
            if (!hasValidPlatform || string.IsNullOrWhiteSpace(playerId)) return;

            // Preserve the raw token verbatim (PlatformRaw) so list-file writes match the game's exact casing.
            PlayerDataRepository.SetPlayerJoining(new() { Platform = platform, PlatformRaw = rawPlatform, PlayerId = playerId });
        }

        private void OnPlayerConnected(params string[] captures)
        {
            var playerName = captures[0];
            var zdoid = captures[1]; // Seems to be a unique object id for the game session
            //var otherNumber = captures[2]; // Not sure what this is for?

            if (string.IsNullOrWhiteSpace(playerName)) return;

            PlayerDataRepository.SetPlayerOnline(playerName, zdoid);
        }

        private void OnPlayerDisconnecting(params string[] captures)
        {
            var playerIdOrZdoId = captures[0];
            if (string.IsNullOrWhiteSpace(playerIdOrZdoId)) return;

            var query = new PlayerDataQuery
            {
                PlayerId = playerIdOrZdoId,
                Or = new()
                {
                    ZdoId = playerIdOrZdoId,
                }
            };

            PlayerDataRepository.SetPlayerLeaving(query);
        }

        private void OnPlayerDisconnected(params string[] captures)
        {
            var playerIdOrZdoId = captures[0];
            if (string.IsNullOrWhiteSpace(playerIdOrZdoId)) return;

            var query = new PlayerDataQuery
            {
                PlayerId = playerIdOrZdoId,
                Or = new()
                {
                    ZdoId = playerIdOrZdoId,
                }
            };

            PlayerDataRepository.SetPlayerOffline(query);
        }

        private void OnPlayerDisconnectedCrossplay(params string[] captures)
        {
            var hasValidPlatform = PlayerPlatforms.TryGetValidPlatform(captures[0], out var platform);
            var playerId = captures[1];
            if (!hasValidPlatform || string.IsNullOrWhiteSpace(playerId)) return;

            PlayerDataRepository.SetPlayerOffline(new() { Platform = platform, PlayerId = playerId });
        }

        private void OnWorldSaved(params string[] captures)
        {
            if (!decimal.TryParse(captures[0], out var timeMs))
            {
                timeMs = 0;
            }

            WorldSaved?.Invoke(this, timeMs);
        }

        private void OnCrossplayJoinCodeAvailable(params string[] captures)
        {
            var inviteCode = captures[0];
            if (string.IsNullOrWhiteSpace(inviteCode)) return;

            InviteCodeReady?.Invoke(this, inviteCode);
        }

        #endregion
    }
}
