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

#pragma warning disable CS0067 // event required by the interface; unused in these tests
    public event EventHandler<PlayerInfoResponse>? PlayerInfoAvailable;
#pragma warning restore CS0067

    public Task RequestPlayerInfoAsync(string platform, string playerId) => Task.CompletedTask;

    public Task SendCrashReportAsync(CrashReport report)
    {
        Reports.Add(report);
        if (ThrowOnSend) throw new InvalidOperationException("network down");
        return Task.CompletedTask;
    }
}
