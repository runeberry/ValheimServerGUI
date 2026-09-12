using System.Collections.Generic;
using ValheimServerGUI.App.Services;

namespace ValheimServerGUI.App.Tests.Services;

/// <summary>Records the launch calls a <see cref="ShellLauncher"/> makes, and fakes the filesystem checks.</summary>
internal sealed class RecordingSystemShell : ISystemShell
{
    public List<(string FileName, string[] Args)> Started { get; } = new();
    public List<string> ShellOpened { get; } = new();
    public HashSet<string> ExistingDirectories { get; } = new();
    public HashSet<string> ExistingFiles { get; } = new();

    /// <summary>When set, the next <see cref="Start"/> throws once (to exercise the xdg-open → gio fallback).</summary>
    public bool ThrowOnceOnStart { get; set; }

    public void Start(string fileName, params string[] arguments)
    {
        if (ThrowOnceOnStart)
        {
            ThrowOnceOnStart = false;
            throw new System.ComponentModel.Win32Exception("no such file");
        }

        Started.Add((fileName, arguments));
    }

    public void ShellOpen(string target) => ShellOpened.Add(target);

    public bool DirectoryExists(string path) => ExistingDirectories.Contains(path);

    public bool FileExists(string path) => ExistingFiles.Contains(path);
}
