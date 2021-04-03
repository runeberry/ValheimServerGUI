using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Game
{
    public interface IPlayerDataRepository : IDataRepository<PlayerInfo>
    {
        event EventHandler<PlayerInfo> PlayerStatusChanged;

        PlayerInfo FindPlayerBySteamId(string steamId, bool createNew = false);

        PlayerInfo FindJoiningPlayerByName(string playerName);

        void SavePlayer(PlayerInfo player);

        void Load(); // todo: find a way to automatically load data without an explicit call
    }

    public class PlayerDataRepository : DataFileRepository<PlayerInfo>, IPlayerDataRepository
    {
        public event EventHandler<PlayerInfo> PlayerStatusChanged;

        private readonly Dictionary<string, PlayerStatus> PlayerStatusMap = new();
        private readonly HashSet<string> PoolOfUncertainty = new();

        public PlayerDataRepository(IDataFileRepositoryContext context)  
            : base(context, Resources.PlayerListFilePath)
        {
            this.DataFileProvider.DataLoaded += this.OnDataLoaded;
            this.EntityUpdated += this.OnEntityUpdated;
        }

        public PlayerInfo FindPlayerBySteamId(string steamId, bool createNew = false)
        {
            // Gonna sneak a little validation in here don't tell nobody :3
            if (string.IsNullOrWhiteSpace(steamId)) return null;

            var player = this.FindById(steamId);

            if (player == null && createNew)
            {
                player = new PlayerInfo
                {
                    SteamId = steamId,
                    PlayerStatus = PlayerStatus.Offline, // Considered offline until otherwise defined
                };
                this.Upsert(player);
            }

            return player;
        }

        public PlayerInfo FindJoiningPlayerByName(string playerName)
        {
            PlayerInfo player;

            // This is tricky becase we don't have the SteamID in this particular log message
            // So consider all players that are currently connecting to the game
            var joiningPlayers = this.Data.Where(p => p.PlayerStatus == PlayerStatus.Joining);
            var joiningWithSameName = joiningPlayers.Where(p => p.PlayerName == playerName);

            if (joiningPlayers.Count() > 1 && joiningWithSameName.Any())
            {
                // If multiple players are joining, we can narrow down the list if any of the joining
                // players already have a name that matches the requested player name
                joiningPlayers = joiningWithSameName;

                this.Logger.LogTrace($"Multiple players joining, but found {joiningPlayers.Count()} match(es) by name {playerName}.");
            }

            if (joiningPlayers.Count() == 1)
            {
                // If there's only one player connecting (or multiple players, but one has the same name), use that player.
                // It's gotta be the same person... right?
                player = joiningPlayers.Single();
                player.SteamIdConfident = true;
                player.SteamIdAlternates = null;

                this.Logger.LogTrace($"Matched PlayerName {playerName} to SteamId {player.SteamId} with high confidence.");
            }
            else
            {
                if (this.ResolveUncertainSteamIds())
                {
                    // Run this method recursively until no more uncertain id updates are made
                    player = this.FindJoiningPlayerByName(playerName);
                }
                else
                {
                    // We tried, but we couldn't resolve this name to a single player.
                    // Simply assume it's the player who's been trying to join the longest.
                    player = joiningPlayers.OrderBy(p => p.LastStatusChange).First();

                    this.Logger.LogWarning(
                        $"Multiple players joining, matched PlayerName {playerName} to SteamId {player.SteamId} with low confidence.");
                }
            }

            return player;
        }

        public void SavePlayer(PlayerInfo player)
        {
            this.Upsert(player);
        }

        #region Non-public methods

        private void OnDataLoaded(object sender, EventArgs args)
        {
            this.PlayerStatusMap.Clear();

            foreach (var player in this.Data)
            {
                this.PlayerStatusMap[player.Key] = player.PlayerStatus;
            }
        }

        private void OnEntityUpdated(object sender, PlayerInfo player)
        {
            bool statusChanged;

            if (this.PlayerStatusMap.TryGetValue(player.Key, out var oldStatus))
            {
                statusChanged = player.PlayerStatus != oldStatus;
            }
            else
            {
                statusChanged = true;
            }

            this.PlayerStatusMap[player.Key] = player.PlayerStatus;

            if (statusChanged)
            {
                if (player.PlayerStatus == PlayerStatus.Joining)
                {
                    // All players that are concurrently joining will be added to the pool
                    this.PoolOfUncertainty.Add(player.SteamId);
                }
                else if (oldStatus == PlayerStatus.Joining && !this.Data.Any(p => p.PlayerStatus == PlayerStatus.Joining))
                {
                    // If this is the last player to *leave* the joining status, clear the pool
                    this.PoolOfUncertainty.Clear();
                }

                this.PlayerStatusChanged?.Invoke(this, player);
            }
        }

        private bool ResolveUncertainSteamIds()
        {
            var anyResolved = false;

            try
            {
                var numResolved = 0;

                do
                {
                    numResolved = this.ResolveUncertainSteamIdsInternal();
                    anyResolved = anyResolved || numResolved > 0;
                }
                while (numResolved > 0);
            }
            catch (Exception e)
            {
                this.Logger.LogWarning(e, "Encountered exception when trying to resolve uncertain steamIds");
            }

            if (anyResolved)
            {
                this.Save();
            }

            return anyResolved;
        }

        private int ResolveUncertainSteamIdsInternal()
        {
            var numResolved = 0;

            // Search any players we've ever encountered where we haven't been able to resolve the steamId (including offline players)
            var uncertainPlayers = this.Data.Where(p => !p.SteamIdConfident);
            if (!uncertainPlayers.Any()) return numResolved;

            // Any of these players may have an incorrect steamId
            var possibleSteamIds = uncertainPlayers.Select(p => p.SteamId).Distinct();

            foreach (var player in uncertainPlayers)
            {
                if (player.SteamIdAlternates == null || !player.SteamIdAlternates.Any())
                {
                    // First pass, the player may have any of these possible ids
                    player.SteamIdAlternates = possibleSteamIds.ToList();
                }
                else
                {
                    // Each time the player connects, the alternates list has the opportunity to get smaller
                    // by looking at all the possible SteamIds in each session until we can find the one
                    // that is always common for this player name
                    var intersection = player.SteamIdAlternates.Intersect(possibleSteamIds);

                    if (intersection.Count() == 1)
                    {
                        // We have found the common steamId for this player name
                        var steamId = intersection.Single();

                        player.SteamIdConfident = true;
                        player.SteamIdAlternates = null;
                        numResolved++;

                        this.Logger.LogInformation("RESOLVED: SteamId {0} belongs to player {1}",
                            steamId, player.PlayerName);

                        if (player.SteamId != steamId)
                        {
                            // Our previous guess was incorrect, swap with the player who currently has this id
                            var otherPlayer = uncertainPlayers.Single(p => p.SteamId == steamId);
                            otherPlayer.SteamId = player.SteamId;
                            player.SteamId = steamId;

                            this.Logger.LogInformation("Previous SteamId {0} was incorrect, swapping with player {1}",
                                steamId, player.PlayerName);
                        }
                    }
                    else if (!intersection.Any())
                    {
                        // The only way this should happen is if a player with the same name was encountered
                        // on a completely different steamId that was also uncertain
                        this.Logger.LogWarning("Unable to resolve SteamId for player {0}. Is this player's name in use by another Steam user?",
                            player.PlayerName);
                    }
                    else
                    {
                        this.Logger.LogTrace("Unable to resolve SteamId for player {1}, may be one of: {0}",
                            string.Join(", ", intersection), player.PlayerName);
                    }
                }
            }

            return numResolved;
        }

        #endregion
    }
}
