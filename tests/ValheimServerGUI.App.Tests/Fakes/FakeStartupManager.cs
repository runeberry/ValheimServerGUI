using System.Collections.Generic;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Tests.Fakes;

internal sealed class FakeStartupManager : IStartupManager
{
    public List<bool> Applied { get; } = new();

    public bool ApplyStartupSetting(bool runOnStartup)
    {
        Applied.Add(runOnStartup);
        return true;
    }
}
