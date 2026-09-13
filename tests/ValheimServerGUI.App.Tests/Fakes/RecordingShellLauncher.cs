using System.Collections.Generic;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>An <see cref="IShellLauncher"/> that records the web addresses and directories it was asked to open.</summary>
internal sealed class RecordingShellLauncher : IShellLauncher
{
    public List<string> OpenedWebAddresses { get; } = new();
    public List<string> OpenedDirectories { get; } = new();

    public void OpenDirectory(string path) => OpenedDirectories.Add(path);

    public void OpenWebAddress(string url) => OpenedWebAddresses.Add(url);
}
