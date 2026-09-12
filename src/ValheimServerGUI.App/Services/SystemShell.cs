using System.Diagnostics;
using System.IO;

namespace ValheimServerGUI.App.Services;

/// <summary>Default <see cref="ISystemShell"/> backed by <see cref="Process"/> and <see cref="File"/>/<see cref="Directory"/>.</summary>
internal sealed class SystemShell : ISystemShell
{
    public void Start(string fileName, params string[] arguments)
    {
        var psi = new ProcessStartInfo(fileName) { UseShellExecute = false };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);
        Process.Start(psi);
    }

    public void ShellOpen(string target)
    {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);
}
