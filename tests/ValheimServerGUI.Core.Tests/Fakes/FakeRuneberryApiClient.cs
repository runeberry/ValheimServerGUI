using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// No-network Runeberry client: records sent crash reports and returns a configurable latest release.
    /// </summary>
    public class FakeRuneberryApiClient : IRuneberryApiClient
    {
        public List<CrashReport> SentReports { get; } = new();

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
    }
}
