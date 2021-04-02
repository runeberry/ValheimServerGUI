using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Tests.Game
{
    public class ValheimServerTests
    {
        private readonly ValheimServer Server;
        private readonly ValheimServerLogger ServerLogger;

        public ValheimServerTests()
        {
            var services = TestServices.Build();

            Server = services.GetRequiredService<ValheimServer>();
            ServerLogger = services.GetRequiredService<ValheimServerLogger>();
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

        #region Helper methods

        private void Log(string message)
        {
            ServerLogger.LogInformation(message);
        }

        #endregion
    }
}
