using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Game
{
    public class PlayerDataQuery
    {
        public string Platform;

        public string PlayerId;

        public string PlayerName;

        public string ZdoId;

        public string CharacterName;

        public PlayerDataQuery Or;

        public override string ToString()
        {
            var parameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(Platform)) parameters.Add($"Platform={Platform}");
            if (!string.IsNullOrWhiteSpace(PlayerId)) parameters.Add($"PlayerId={PlayerId}");
            if (!string.IsNullOrWhiteSpace(PlayerName)) parameters.Add($"PlayerName={PlayerName}");
            if (!string.IsNullOrWhiteSpace(ZdoId)) parameters.Add($"ZdoId={ZdoId}");
            if (!string.IsNullOrWhiteSpace(CharacterName)) parameters.Add($"CharacterName={CharacterName}");

            var qs = string.Join("&", parameters);

            if (Or != null)
            {
                var qs2 = Or.ToString();
                if (!string.IsNullOrWhiteSpace(qs2))
                {
                    qs = $"{qs}|{qs2}";
                }
            }

            return qs;
        }

        public bool HasParameters() => !string.IsNullOrWhiteSpace(ToString());
    }

    public interface IPlayerDataRepository : IDataRepository<PlayerInfo>
    {
        event EventHandler<PlayerInfo> PlayerStatusChanged;

        IEnumerable<PlayerInfo> FindPlayersByQuery(PlayerDataQuery query);

        PlayerInfo SetPlayerJoining(PlayerDataQuery query);

        PlayerInfo SetPlayerOnline(string characterName, string zdoId);

        void SetPlayerLeaving(PlayerDataQuery query);

        void SetPlayerOffline(PlayerDataQuery query);

        Task LoadAsync(); // todo: find a way to automatically load data without exposing this
    }

    public class PlayerDataRepository : DataFileRepository<PlayerInfo>, IPlayerDataRepository
    {
        public event EventHandler<PlayerInfo> PlayerStatusChanged;

        private readonly IRuneberryApiClient RuneberryApiClient;
        private readonly Dictionary<string, PlayerStatus> PlayerStatusMap = new();
        private readonly Dictionary<string, DateTimeOffset> LastOfflineCache = new();

        public PlayerDataRepository(
            IDataFileRepositoryContext context,
            IRuneberryApiClient runeberryApiClient)
            : base(context, Resources.PlayerListFilePath)
        {
            EntityUpdated += OnEntityUpdated;
            RuneberryApiClient = runeberryApiClient;
            RuneberryApiClient.PlayerInfoAvailable += OnPlayerInfoAvailable;
        }

        #region IPlayerDataRepository implementation

        public IEnumerable<PlayerInfo> FindPlayersByQuery(PlayerDataQuery query)
        {
            var results = Data;

            if (!string.IsNullOrWhiteSpace(query.Platform))
            {
                results = results.Where(p => p.Platform == query.Platform);
            }

            if (!string.IsNullOrWhiteSpace(query.PlayerId))
            {
                results = results.Where(p => p.PlayerId == query.PlayerId);
            }

            if (!string.IsNullOrWhiteSpace(query.PlayerName))
            {
                results = results.Where(p => p.PlayerName == query.PlayerName);
            }

            if (!string.IsNullOrWhiteSpace(query.ZdoId))
            {
                results = results.Where(p => p.ZdoId == query.ZdoId);
            }

            if (!string.IsNullOrWhiteSpace(query.CharacterName))
            {
                results = results.Where(p => p.Characters != null && p.Characters.Any(c => c.CharacterName == query.CharacterName));
            }

            if (query.Or != null)
            {
                var results2 = FindPlayersByQuery(query.Or);

                results = results
                    .Concat(results2)
                    .DistinctBy(p => p.Key);
            }

            return results;
        }

        public PlayerInfo SetPlayerJoining(PlayerDataQuery query)
        {
            if (!query.HasParameters()) return null;

            // Retrieve the player w/ this identity who was most recently online
            var player = FindPlayersByQuery(query)
                .Where(p => p.PlayerStatus == PlayerStatus.Offline)
                .OrderByDescending(p => p.LastStatusChange)
                .FirstOrDefault();

            if (player == null)
            {
                // If we have no offline record of this player, create a new record
                player = CreatePlayerFromQuery(query);
                Logger.Information("1 new player joining as: {query}", query);
            }
            else
            {
                Logger.Information("1 existing player joining as: {query}", query);
            }

            player.PlayerStatus = PlayerStatus.Joining;
            player.LastStatusChange = DateTime.UtcNow;
            player.LastStatusCharacter = !string.IsNullOrWhiteSpace(query.CharacterName) ? query.CharacterName : null;
            Upsert(player);

            if (string.IsNullOrWhiteSpace(player.PlayerName))
            {
                RuneberryApiClient.RequestPlayerInfoAsync(player.Platform, player.PlayerId);
            }

            return player;
        }

        public PlayerInfo SetPlayerOnline(string characterName, string zdoId)
        {
            PlayerInfo player = null;
            var playersToSave = new List<PlayerInfo>();

            var playersWithCharName = Enumerable.Empty<PlayerInfo>();
            if (!string.IsNullOrWhiteSpace(characterName))
            {
                playersWithCharName = FindPlayersByQuery(new() { CharacterName = characterName })
                    .Where(p => p.PlayerStatus.IsAnyValue(PlayerStatus.Joining, PlayerStatus.Offline));
            }

            var joiningPlayers = Data
                .Where(p => p.PlayerStatus == PlayerStatus.Joining);

            var matchConfident = true;

            if (playersWithCharName.Count() == 1)
            {
                // One player record with this name has been seen in the past with this name, so use it
                player = playersWithCharName.Single();
                Logger.Information("(PlayerOnline) Character {name} belongs to player {key} (Single match by name)", characterName, player.Key);

                if (player.PlayerStatus == PlayerStatus.Offline)
                {
                    // If this record with this matching name was offline, then the same player
                    // is logging in with a different name than the one we picked when they were joining
                    var wrongJoiningPlayer = Data.Where(p => p.Key == player.Key && p.PlayerStatus == PlayerStatus.Joining).FirstOrDefault();

                    // If there was somehow no joining player match, just use this player anyway
                    if (wrongJoiningPlayer != null)
                    {
                        wrongJoiningPlayer.PlayerStatus = PlayerStatus.Offline;
                        if (LastOfflineCache.TryGetValue(player.Key, out var origTimestamp))
                        {
                            wrongJoiningPlayer.LastStatusChange = origTimestamp;
                        }
                        playersToSave.Add(wrongJoiningPlayer);

                        Logger.Information("Player {key} was not joining after all, reverting to offline status", wrongJoiningPlayer.Key);
                    }
                }
            }
            else if (playersWithCharName.Count() > 1)
            {
                // Multiple offline or joining players have been detected with the same name
                var joiningPlayersWithSameName = playersWithCharName
                    .Where(p => p.PlayerStatus == PlayerStatus.Joining);

                if (joiningPlayersWithSameName.Count() == 1)
                {
                    // If one of those is joining, assume that's the right player
                    player = joiningPlayersWithSameName.Single();
                    Logger.Information("(PlayerOnline) Character {name} belongs to player {key} (Single joining by name)", characterName, player.Key);
                }
                else
                {
                    // Otherwise, we cannot reliably pick which of the players by this name we should update
                    Logger.Information("Cannot resolve identity for character {name} (Multiple joining w/ same name)", characterName);
                }
            }
            else if (joiningPlayers.Count() == 1)
            {
                // No players were found joining or offline w/ this name, but there is only one joining player, so we can confirm this match
                player = joiningPlayers.Single();
                Logger.Information("(PlayerOnline) Character {name} belongs to player {key} (Single player joining)", characterName, player.Key);
            }
            else if (joiningPlayers.Count() > 1)
            {
                // No players were found joining or offline w/ this name, and multiple players are joining at once (common case on a new server)
                // Assume that this character belongs to the earliest joining player, but flag the match as low-confidence.
                player = joiningPlayers.OrderBy(p => p.LastStatusChange).First();
                matchConfident = false;
                Logger.Information("Ambiguous identity for character {name} (Multiple players joining, no match by name, best guess: {key})", characterName, player.Key);
            }
            else
            {
                Logger.Information("Cannot resolve identity for character {name} (No players joining, no match by name)", characterName);
            }

            if (player != null)
            {
                // If we could actually determine which player to update, do it now
                player.PlayerStatus = PlayerStatus.Online;
                player.LastStatusChange = DateTime.UtcNow;
                player.LastStatusCharacter = characterName;
                player.ZdoId = zdoId;
                player.AddCharacter(characterName, matchConfident);
                playersToSave.Add(player);
            }

            if (playersToSave.Any())
            {
                UpsertBulk(playersToSave);
                Logger.Information("{count} player(s) saved", playersToSave.Count);
            }

            return player;
        }

        public void SetPlayerLeaving(PlayerDataQuery query)
        {
            var players = FindPlayersByQuery(query)
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

                Logger.Information("{count} player(s) leaving as: {query}", players.Count, query);
            }
        }

        public void SetPlayerOffline(PlayerDataQuery query)
        {
            var players = FindPlayersByQuery(query)
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

                Logger.Information("{count} player(s) offline as: {query}", players.Count, query);
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

        #endregion

        #region Non-public methods

        private static PlayerInfo CreatePlayerFromQuery(PlayerDataQuery query, PlayerInfo player = null)
        {
            player ??= new PlayerInfo();

            if (query.Or != null)
            {
                player = CreatePlayerFromQuery(query.Or, player);
            }

            if (!string.IsNullOrWhiteSpace(query.Platform))
            {
                player.Platform = query.Platform;
            }

            if (!string.IsNullOrWhiteSpace(query.PlayerId))
            {
                player.PlayerId = query.PlayerId;
            }

            if (!string.IsNullOrWhiteSpace(query.PlayerName))
            {
                player.PlayerName = query.PlayerName;
            }

            if (!string.IsNullOrWhiteSpace(query.ZdoId))
            {
                player.ZdoId = query.ZdoId;
            }

            if (!string.IsNullOrWhiteSpace(query.CharacterName))
            {
                player.AddCharacter(query.CharacterName);
            }

            return player;
        }

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

        private void OnPlayerInfoAvailable(object sender, PlayerInfoResponse response)
        {
            if (string.IsNullOrWhiteSpace(response.Id)
                || string.IsNullOrWhiteSpace(response.Name)
                || string.IsNullOrWhiteSpace(response.Platform))
            {
                return;
            }

            var query = new PlayerDataQuery
            {
                Platform = response.Platform,
                PlayerId = response.Id,
            };

            var players = FindPlayersByQuery(query);
            foreach (var player in players)
            {
                player.PlayerName = response.Name;
                Logger.Information($"Mapped player name from API: {player.Key}");
            }

            UpsertBulk(players);
        }

        #endregion
    }
}
