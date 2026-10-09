using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// No-op Runeberry client: player-name lookups never resolve, so tests exercise the offline
    /// name-degradation path (E22) deterministically without any network. Records sent crash reports.
    /// </summary>
    public class FakeRuneberryApiClient : IRuneberryApiClient
    {
        public event EventHandler<PlayerInfoResponse>? PlayerInfoAvailable;

        public List<CrashReport> SentReports { get; } = new();

        public Task RequestPlayerInfoAsync(string platform, string playerId) => Task.CompletedTask;

        public Task SendCrashReportAsync(CrashReport report)
        {
            SentReports.Add(report);
            return Task.CompletedTask;
        }

        /// <summary>What <see cref="GetLatestReleaseAsync"/> returns (null = no qualifying release).</summary>
        public LatestReleaseResponse? LatestRelease { get; set; }

        /// <summary>When set, <see cref="GetLatestReleaseAsync"/> throws it (the API was unreachable).</summary>
        public Exception? LatestReleaseError { get; set; }

        public Task<LatestReleaseResponse?> GetLatestReleaseAsync()
            => LatestReleaseError is { } e ? Task.FromException<LatestReleaseResponse?>(e) : Task.FromResult(LatestRelease);

        /// <summary>Test hook: simulate the backend returning a player name.</summary>
        public void RaisePlayerInfo(PlayerInfoResponse response) => PlayerInfoAvailable?.Invoke(this, response);
    }
}
