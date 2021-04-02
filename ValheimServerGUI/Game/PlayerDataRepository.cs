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

        bool ResolveUncertainSteamIds();

        void Load(); // todo: find a way to automatically load data without an explicit call
    }

    public class PlayerDataRepository : DataFileRepository<PlayerInfo>, IPlayerDataRepository
    {
        public event EventHandler<PlayerInfo> PlayerStatusChanged;

        private readonly Dictionary<string, PlayerStatus> PlayerStatusMap = new();

        public PlayerDataRepository(IDataFileRepositoryContext context)  
            : base(context, Resources.PlayerListFilePath)
        {
            this.DataFileProvider.DataLoaded += this.OnDataLoaded;
            this.EntityUpdated += this.OnEntityUpdated;
        }

        public bool ResolveUncertainSteamIds()
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
                this.PlayerStatusChanged?.Invoke(this, player);
            }
        }

        private int ResolveUncertainSteamIdsInternal()
        {
            var numResolved = 0;

            // Search any players we've ever encountered where we haven't been able to resolve the steamId
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
