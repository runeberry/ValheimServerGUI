using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Tests.Game
{
    public class ValheimServerTests
    {
        private const string MessageJoining = "Got connection SteamID {0}";
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

            Assert.Equal(steamId, eventPlayer?.SteamId);
        }

        [Fact]
        public void CanDetectPlayerLeaving()
        {
            var steamId = "1234";
            PlayerInfo eventPlayer = null;
            PlayerDataRepository.PlayerStatusChanged += (_, player) =>
            {
                if (player.PlayerStatus == PlayerStatus.Leaving) eventPlayer = player;
            };

            Log(MessageJoining, steamId);
            Log(MessageWrongPassword, steamId);

            Assert.Equal(steamId, eventPlayer?.SteamId);
        }

        [Fact]
        public void CanDetectPlayerOffline()
        {
            var steamId = "1234";
            PlayerInfo eventPlayer = null;
            PlayerDataRepository.PlayerStatusChanged += (_, player) =>
            {
                if (player.PlayerStatus == PlayerStatus.Offline) eventPlayer = player;
            };

            Log(MessageJoining, steamId);
            Log(MessageOffline, steamId);

            Assert.Equal(steamId, eventPlayer?.SteamId);
        }

        #region Helper methods

        private void Log(string message, params object[] args)
        {
            ServerLogger.LogInformation(string.Format(message, args));
        }

        #endregion
    }
}
