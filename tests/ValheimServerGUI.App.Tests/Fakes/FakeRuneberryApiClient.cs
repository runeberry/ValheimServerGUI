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

    /// <summary>Number of times <see cref="RequestPlayerInfoAsync"/> was called.</summary>
    public int RequestPlayerInfoCallCount { get; private set; }

    /// <summary>Optional hook run on each lookup, so a test can simulate the resolved name reaching the repo.</summary>
    public Func<string, string, Task>? OnRequestPlayerInfo { get; set; }

#pragma warning disable CS0067 // event required by the interface; unused in these tests
    public event EventHandler<PlayerInfoResponse>? PlayerInfoAvailable;
#pragma warning restore CS0067

    public Task RequestPlayerInfoAsync(string platform, string playerId)
    {
        RequestPlayerInfoCallCount++;
        return OnRequestPlayerInfo?.Invoke(platform, playerId) ?? Task.CompletedTask;
    }

    public Task SendCrashReportAsync(CrashReport report)
    {
        Reports.Add(report);
        if (ThrowOnSend) throw new InvalidOperationException("network down");
        return Task.CompletedTask;
    }
}
