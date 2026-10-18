using System.Collections.Generic;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Tests.Fakes;

internal sealed class FakeStartupManager : IStartupManager
{
    /// <summary>The registration the OS currently holds; <see cref="ApplyStartupSetting"/> updates it.</summary>
    public bool Enabled { get; set; }

    public List<bool> Applied { get; } = new();

    public bool IsStartupEnabled() => Enabled;

    public bool ApplyStartupSetting(bool runOnStartup)
    {
        Applied.Add(runOnStartup);
        Enabled = runOnStartup;
        return true;
    }
}
