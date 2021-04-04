using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Tests.Game
{
    public class ValheimServerTests
    {
        private const string MessageJoining = "Got connection SteamID {0}";
        private const string MessageOnline = "Got character ZDOID from {0} : {1}:1";
        private const string MessageWrongPassword = "Peer {0} has wrong password";
        private const string MessageOffline = "Closing socket {0}";

        private readonly ValheimServer Server;
        private readonly ValheimServerLogger ServerLogger;
        private readonly IPlayerDataRepository PlayerDataRepository;

        public ValheimServerTests()
        {
            var services = TestServices.Build();

            Server = services.GetRequiredService<ValheimServer>();
            ServerLogger = services.GetRequiredService<ValheimServerLogger>();
            PlayerDataRepository = services.GetRequiredService<IPlayerDataRepository>();
        }

        [Fact]
        public void ServerIsStoppedInitially()
        {
            Assert.Equal(ServerStatus.Stopped, Server.Status);
        }

        [Fact]
        public void CanDetectServerRunning()
        {
            ServerStatus? eventStatus = null;
            Server.StatusChanged += (_, status) => eventStatus = status;

            Log("Game server connected");

            Assert.Equal(ServerStatus.Running, Server.Status);
            Assert.Equal(ServerStatus.Running, eventStatus);
        }

        [Fact]
        public void CanDetectPlayerJoining()
        {
            var steamId = "1234";
            PlayerInfo eventPlayer = null;
            PlayerDataRepository.PlayerStatusChanged += (_, player) => eventPlayer = player;

            Log(MessageJoining, steamId);

            var expected = new PlayerInfo { SteamId = steamId, PlayerStatus = PlayerStatus.Joining };
            AssertMatch(expected, eventPlayer);

            var dataPlayer = Assert.Single(this.PlayerDataRepository.Data);
            AssertMatch(expected, dataPlayer);
        }

        [Fact]
        public void CanDetectPlayerLeaving()
        {
            var steamId = "1234";
            Log(MessageJoining, steamId);
            PlayerInfo eventPlayer = null;
            PlayerDataRepository.PlayerStatusChanged += (_, player) => eventPlayer = player;

            Log(MessageWrongPassword, steamId);

            var expected = new PlayerInfo { SteamId = steamId, PlayerStatus = PlayerStatus.Leaving };
            AssertMatch(expected, eventPlayer);

            var dataPlayer = Assert.Single(this.PlayerDataRepository.Data);
            AssertMatch(expected, dataPlayer);
        }

        [Fact]
        public void CanDetectPlayerOffline()
        {
            var steamId = "1234";
            Log(MessageJoining, steamId);
            PlayerInfo eventPlayer = null;
            PlayerDataRepository.PlayerStatusChanged += (_, player) => eventPlayer = player;

            Log(MessageOffline, steamId);

            var expected = new PlayerInfo { SteamId = steamId, PlayerStatus = PlayerStatus.Offline };
            AssertMatch(expected, eventPlayer);

            var dataPlayer = Assert.Single(this.PlayerDataRepository.Data);
            AssertMatch(expected, dataPlayer);
        }

        // If only one player is joining, and that player goes online,
        // then that one player's name and steamId should match up.
        [Fact]
        public void CanMatchPlayerNameSingle()
        {
            // If only one player by steamId is joining...
            var steamId = "1234";
            Log(MessageJoining, steamId);

            PlayerInfo eventPlayer = null;
            PlayerDataRepository.PlayerStatusChanged += (_, player) => eventPlayer = player;

            // And a player by name goes online...
            var (name, zdoid) = ("Broheim", "-56789123");
            Log(MessageOnline, name, zdoid);

            // ...then that player name and steamId are confidently matched up.
            var expected = CreatePlayer(steamId, name, PlayerStatus.Online, zdoid);
            AssertMatch(expected, eventPlayer);

            var dataPlayer = Assert.Single(this.PlayerDataRepository.Data);
            AssertMatch(expected, dataPlayer);
        }

        // If multiple players are joining at the same time, and no names are known,
        // then nobody in the group will go online until all joining players have connected
        [Fact]
        public void CanDelayOnlineStatusForAnonymousPlayers()
        {
            // If multiple players are joining by steamId at the same time...
            var ids = new[] { "123", "456", "789" };
            Log(MessageJoining, ids[0]);
            Log(MessageJoining, ids[1]);
            Log(MessageJoining, ids[2]);

            PlayerInfo eventPlayer = null;
            var eventCalls = 0;
            PlayerDataRepository.PlayerStatusChanged += (_, player) =>
            {
                eventCalls++;
                eventPlayer = player;
            };

            Assert.Equal(3, this.PlayerDataRepository.Data.Count());

            // And a player by name goes online...
            var (name1, zdoid1) = ("PlayerOne", "-432");
            Log(MessageOnline, name1, zdoid1);

            // ...then that player is not yet brought online
            Assert.Null(eventPlayer);
            Assert.Equal(0, eventCalls);
            Assert.True(this.PlayerDataRepository.Data.All(p => p.PlayerStatus == PlayerStatus.Joining));

            // And another player goes online...
            var (name2, zdoid2) = ("PlayerTwo", "5834");
            Log(MessageOnline, name2, zdoid2);

            // ...then *still* no players are brought online
            Assert.Null(eventPlayer);
            Assert.Equal(0, eventCalls);
            Assert.True(this.PlayerDataRepository.Data.All(p => p.PlayerStatus == PlayerStatus.Joining));

            // And the final player goes online...
            var (name3, zdoid3) = ("PlayerThree", "146131");
            Log(MessageOnline, name3, zdoid3);

            // ...then, finally, all players are brought online
            Assert.NotNull(eventPlayer);
            Assert.Equal(3, eventCalls);
            Assert.True(this.PlayerDataRepository.Data.All(p => p.PlayerStatus == PlayerStatus.Online));
        }

        // If the same steamId joins under multiple character names, then we can track separate
        // records for each character name
        [Fact]
        public void CanTrackMultipleCharactersForSameSteamId()
        {
            // If a player has joined in the past...
            var steamId = "1234";
            var (name1, zdoid1) = ("PlayerOne", "234");
            Log(MessageJoining, steamId);
            Log(MessageOnline, name1, zdoid1);
            Log(MessageOffline, steamId);

            PlayerInfo eventPlayer = null;
            PlayerInfo dataPlayer;
            PlayerInfo expected;
            PlayerDataRepository.PlayerStatusChanged += (_, player) => eventPlayer = player;

            dataPlayer = this.PlayerDataRepository.Data.Single();
            var origTimestamp = dataPlayer.LastStatusChange; // For later assertion

            // And joins again...
            Log(MessageJoining, steamId);

            // ...then that player is marked as Joining
            expected = CreatePlayer(steamId, name1, PlayerStatus.Joining);
            AssertMatch(expected, eventPlayer);

            dataPlayer = Assert.Single(this.PlayerDataRepository.Data);
            AssertMatch(expected, dataPlayer);
            var origKey = dataPlayer.Key; // For lookup in later assertion

            // And that player connects under a different name...
            var (name2, zdoid2) = ("PlayerTwo", "567");
            Log(MessageOnline, name2, zdoid2);

            // ...then a new player is published, and the old one is reverted to Offline
            expected = CreatePlayer(steamId, name2, PlayerStatus.Online, zdoid2);
            AssertMatch(expected, eventPlayer);

            Assert.Equal(2, this.PlayerDataRepository.Data.Count());
            dataPlayer = this.PlayerDataRepository.FindById(eventPlayer.Key);
            AssertMatch(expected, dataPlayer);

            // ...then the offline player should have had their timestamp reverted
            dataPlayer = this.PlayerDataRepository.FindById(origKey);
            Assert.Equal(origTimestamp, dataPlayer.LastStatusChange);

            // And that player goes offline...
            Log(MessageOffline, steamId);

            // ...then the original player's timestamp is unaffected
            dataPlayer = this.PlayerDataRepository.FindById(origKey);
            Assert.Equal(origTimestamp, dataPlayer.LastStatusChange);
        }

        #region Helper methods

        private void Log(string message, params object[] args)
        {
            ServerLogger.LogInformation(string.Format(message, args));
        }

        private static PlayerInfo CreatePlayer(
            string steamId, 
            string name = null,
            PlayerStatus? status = null,
            string zdoid = null,
            bool confident = false,
            string change = null
        )
        {
            return new PlayerInfo
            {
                SteamId = steamId,
                PlayerName = name,
                PlayerStatus = status ?? PlayerStatus.Offline,
                LastStatusChange = change != null ? DateTime.Parse(change) : GetNextTimestamp(),
                ZdoId = zdoid,
                MatchConfident = confident
            };
        }

        private static int TimeCounter = 0;

        private static DateTime GetNextTimestamp()
        {
            return DateTime.Parse("01-01-2021").Add(TimeSpan.FromHours(++TimeCounter));
        }

        private static void AssertMatch(PlayerInfo expected, PlayerInfo actual)
        {
            Assert.NotNull(actual);
            
            Assert.Equal(expected.SteamId, actual.SteamId);
            Assert.Equal(expected.PlayerStatus, actual.PlayerStatus);

            if (expected.PlayerName != null) Assert.Equal(expected.PlayerName, actual.PlayerName);
            if (expected.ZdoId != null) Assert.Equal(expected.ZdoId, actual.ZdoId);
        }

        #endregion
    }
}
