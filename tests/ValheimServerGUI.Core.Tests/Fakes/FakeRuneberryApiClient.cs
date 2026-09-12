using System;
using System.Threading.Tasks;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// No-op Runeberry client: player-name lookups never resolve, so tests exercise the offline
    /// name-degradation path (E22) deterministically without any network.
    /// </summary>
    public class FakeRuneberryApiClient : IRuneberryApiClient
    {
        public event EventHandler<PlayerInfoResponse>? PlayerInfoAvailable;

        public Task RequestPlayerInfoAsync(string platform, string playerId) => Task.CompletedTask;

        public Task SendCrashReportAsync(CrashReport report) => Task.CompletedTask;

        /// <summary>Test hook: simulate the backend returning a player name.</summary>
        public void RaisePlayerInfo(PlayerInfoResponse response) => PlayerInfoAvailable?.Invoke(this, response);
    }
}
