using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.Tests.Fakes;

internal sealed class FakeRuneberryApiClient : IRuneberryApiClient
{
    public List<CrashReport> Reports { get; } = new();
    public bool ThrowOnSend { get; set; }

    public Task<LatestReleaseResponse?> GetLatestReleaseAsync() => Task.FromResult<LatestReleaseResponse?>(null);

    public Task SendCrashReportAsync(CrashReport report)
    {
        Reports.Add(report);
        if (ThrowOnSend) throw new InvalidOperationException("network down");
        return Task.CompletedTask;
    }
}
