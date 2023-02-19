using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Game
{
    public interface IPlayerDataRepository : IDataRepository<PlayerInfo>
    {
        event EventHandler<PlayerInfo> PlayerStatusChanged;

        PlayerInfo SetPlayerJoining(string steamId);

        PlayerInfo SetPlayerOnline(string name, string zdoid);

        void SetPlayerLeaving(string steamOrZdoId);

        void SetPlayerOffline(string steamOrZdoId);

        Task LoadAsync(); // todo: find a way to automatically load data without exposing this
    }

    public class PlayerDataRepository : DataFileRepository<PlayerInfo>, IPlayerDataRepository
    {
        public event EventHandler<PlayerInfo> PlayerStatusChanged;

        private readonly Dictionary<string, PlayerStatus> PlayerStatusMap = new();
        private readonly Dictionary<string, DateTimeOffset> LastOfflineCache = new();
        private int JoinCount;

        public PlayerDataRepository(IDataFileRepositoryContext context)
            : base(context, Resources.PlayerListFilePath)
        {
            EntityUpdated += OnEntityUpdated;
        }

        public PlayerInfo SetPlayerJoining(string steamId)
        {
            if (string.IsNullOrWhiteSpace(steamId)) return null;

            // Retrieve the player w/ this SteamID who was most recently online
            var players = Data
                .Where(p => p.SteamId == steamId && p.PlayerStatus == PlayerStatus.Offline)
                .OrderByDescending(p => p.LastStatusChange);

            var player = players.FirstOrDefault();

            if (player == null)
            {
                // If we have no offline record of this player, create a new record
                player = new PlayerInfo { SteamId = steamId };

                Logger.LogInformation("1 player joining for SteamID {id} (new player)", steamId);
            }
            else
            {
                Logger.LogInformation("1 player joining for SteamID {id} (existing player)", steamId);
            }

            player.PlayerStatus = PlayerStatus.Joining;
            player.LastStatusChange = DateTime.UtcNow;
            JoinCount++;
            Upsert(player);

            return player;
        }

        public PlayerInfo SetPlayerOnline(string name, string zdoid)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            PlayerInfo player = null;
            var playersToSave = new List<PlayerInfo>();
            var playersToRemove = new List<string>();

            var playersWithSameName = Data
                .Where(p => p.PlayerName == name && p.PlayerStatus.IsAnyValue(PlayerStatus.Joining, PlayerStatus.Offline));

            var joiningPlayers = Data
                .Where(p => p.PlayerStatus == PlayerStatus.Joining);

            if (playersWithSameName.Count() == 1)
            {
                // One player record with this name has been seen in the past with this name, so use it
                player = playersWithSameName.Single();
                Logger.LogInformation("(PlayerOnline) Player {name} belongs to SteamID {id} (Single match by name)", name, player.SteamId);

                if (player.PlayerStatus == PlayerStatus.Offline)
                {
                    // If this record with this matching name was offline, then same steam player
                    // is logging in with a different name than the one we picked when they were joining
                    var wrongJoiningPlayer = Data.Where(p => p.SteamId == player.SteamId && p.PlayerStatus == PlayerStatus.Joining).FirstOrDefault();

                    // If there was somehow no joining steamId match, just use this player anyway
                    if (wrongJoiningPlayer != null)
                    {
                        wrongJoiningPlayer.PlayerStatus = PlayerStatus.Offline;
                        if (LastOfflineCache.TryGetValue(player.Key, out var origTimestamp))
                        {
                            wrongJoiningPlayer.LastStatusChange = origTimestamp;
                        }
                        playersToSave.Add(wrongJoiningPlayer);

                        Logger.LogInformation("Player {name} was not logging in after all, reverting to offline status", wrongJoiningPlayer.PlayerName);
                    }
                }
            }
            else if (playersWithSameName.Count() > 1)
            {
                // Multiple offline or joining players have been detected with the same name
                var joiningPlayersWithSameName = playersWithSameName
                    .Where(p => p.PlayerStatus == PlayerStatus.Joining);

                if (joiningPlayersWithSameName.Count() == 1)
                {
                    // If one of those is joining, assume that's the right player
                    player = joiningPlayersWithSameName.Single();
                    Logger.LogInformation("(PlayerOnline) Player {name} belongs to SteamID {id} (Single joining by name)", name, player.SteamId);
                }
                else
                {
                    // Otherwise, we cannot reliably pick which of the players by this name we should update
                    Logger.LogInformation("Cannot resolve SteamID for Player {name} (Multiple joining w/ same name)", name);
                }
            }
            else if (joiningPlayers.Count() == 1)
            {
                // No players were found joining or offline w/ this name, but there is only one joining player, so we can confirm this match
                player = joiningPlayers.Single();
                Logger.LogInformation("(PlayerOnline) Player {name} belongs to SteamID {id} (Single player joining)", name, player.SteamId);

                if (player.PlayerName == null)
                {
                    playersToRemove.Add(player.Key);
                    player.PlayerName = name;
                }
                else if (player.PlayerName != name)
                {
                    // Make a new record for each character that a SteamID logs in with, set the old record back to offline
                    player.PlayerStatus = PlayerStatus.Offline;
                    if (LastOfflineCache.TryGetValue(player.Key, out var origTimestamp))
                    {
                        player.LastStatusChange = origTimestamp;
                    }
                    playersToSave.Add(player);

                    Logger.LogInformation("Player {name} was not logging in after all, reverting to offline status", player.PlayerName);

                    var steamId = player.SteamId;
                    player = new PlayerInfo { SteamId = steamId, PlayerName = name };
                }
            }
            else
            {
                Logger.LogInformation("Cannot resolve SteamID for Player {name} (Multiple players joining, no match by name)", name);
            }

            if (player != null)
            {
                // If we could actually determine which player to update, do it now
                player.PlayerStatus = PlayerStatus.Online;
                player.LastStatusChange = DateTime.UtcNow;
                player.MatchConfident = true;
                player.ZdoId = zdoid;
                playersToSave.Add(player);
            }

            if (--JoinCount == 0)
            {
                // Once all joining players have come online, update the remaining anonymous players to an online status
                var remainingPlayers = Data.Where(p => p.PlayerStatus == PlayerStatus.Joining);

                if (remainingPlayers.Any())
                {
                    foreach (var jp in remainingPlayers)
                    {
                        jp.PlayerStatus = PlayerStatus.Online;
                        playersToSave.Add(jp);
                    }

                    Logger.LogInformation("(PlayerOnline) {count} anonymous player(s) entering Online status", remainingPlayers.Count());
                }
            }

            if (playersToRemove.Any())
            {
                RemoveBulk(playersToRemove);
                Logger.LogInformation("{count} player(s) removed", playersToSave.Count);
            }

            if (playersToSave.Any())
            {
                UpsertBulk(playersToSave);
                Logger.LogInformation("{count} player(s) saved", playersToSave.Count);
            }

            return player;
        }

        public void SetPlayerLeaving(string steamOrZdoId)
        {
            var players = Data
                .Where(p => p.SteamId == steamOrZdoId || p.ZdoId == steamOrZdoId)
                .Where(p => p.PlayerStatus.IsAnyValue(PlayerStatus.Joining, PlayerStatus.Online))
                .ToList();

            if (players.Any())
            {
                foreach (var player in players)
                {
                    player.PlayerStatus = PlayerStatus.Leaving;
                    player.LastStatusChange = DateTime.UtcNow;
                    player.ZdoId = null;
                }

                UpsertBulk(players);

                Logger.LogInformation("{count} player(s) Leaving for SteamID {id}", players.Count, steamOrZdoId);
            }
        }

        public void SetPlayerOffline(string steamOrZdoId)
        {
            var players = Data
                .Where(p => p.SteamId == steamOrZdoId || p.ZdoId == steamOrZdoId)
                .Where(p => p.PlayerStatus != PlayerStatus.Offline)
                .ToList();

            if (players.Any())
            {
                foreach (var player in players)
                {
                    player.PlayerStatus = PlayerStatus.Offline;
                    player.LastStatusChange = DateTime.UtcNow;
                    player.ZdoId = null;
                }

                UpsertBulk(players);

                Logger.LogInformation("{count} player(s) Offline for SteamID {id}", players.Count, steamOrZdoId);
            }
        }

        public override async Task LoadAsync()
        {
            await base.LoadAsync();

            PlayerStatusMap.Clear();

            foreach (var player in Data)
            {
                PlayerStatusMap[player.Key] = player.PlayerStatus;
                LastOfflineCache[player.Key] = player.LastStatusChange;
            }
        }

        #region Non-public methods

        private void OnEntityUpdated(object sender, PlayerInfo player)
        {
            bool statusChanged;

            if (PlayerStatusMap.TryGetValue(player.Key, out var oldStatus))
            {
                statusChanged = player.PlayerStatus != oldStatus;

                if (statusChanged)
                {
                    // If the player enters the Offline status, we need to cache that time to reference later
                    if (player.PlayerStatus == PlayerStatus.Offline)
                    {
                        LastOfflineCache[player.Key] = player.LastStatusChange;
                    }
                }
            }
            else
            {
                statusChanged = true;
            }

            PlayerStatusMap[player.Key] = player.PlayerStatus;

            if (statusChanged)
            {
                PlayerStatusChanged?.Invoke(this, player);
            }
        }

        #endregion
    }
}
