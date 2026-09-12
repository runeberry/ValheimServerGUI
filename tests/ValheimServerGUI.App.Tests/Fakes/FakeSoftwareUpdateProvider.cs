using System;
using System.Threading.Tasks;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>Records update checks; can be told to throw to exercise startup resilience.</summary>
internal sealed class FakeSoftwareUpdateProvider : ISoftwareUpdateProvider
{
    public int CheckCount { get; private set; }
    public bool? LastIsManual { get; private set; }
    public bool ThrowOnCheck { get; set; }

    public event EventHandler? UpdateCheckStarted;
    public event EventHandler<SoftwareUpdateEventArgs>? UpdateCheckFinished;

    public Task CheckForUpdatesAsync(bool isManualCheck)
    {
        CheckCount++;
        LastIsManual = isManualCheck;
        UpdateCheckStarted?.Invoke(this, EventArgs.Empty);
        if (ThrowOnCheck) throw new InvalidOperationException("no network");
        return Task.CompletedTask;
    }

    // Kept to satisfy the interface + silence unused-event warnings.
    public void RaiseFinished(SoftwareUpdateEventArgs args) => UpdateCheckFinished?.Invoke(this, args);
}
