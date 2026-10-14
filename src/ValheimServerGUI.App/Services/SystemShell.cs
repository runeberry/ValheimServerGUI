using System.Diagnostics;
using System.IO;

namespace ValheimServerGUI.App.Services;

/// <summary>Default <see cref="ISystemShell"/> backed by <see cref="Process"/> and <see cref="File"/>/<see cref="Directory"/>.</summary>
internal sealed class SystemShell : ISystemShell
{
    public void Start(string fileName, params string[] arguments)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            // A spawned Linux opener (xdg-open -> the file manager) otherwise inherits our console and
            // spams GTK accessibility-bus + label warnings. Detach its stdio and skip the a11y probe so its
            // noise never reaches our output. (The GtkLabel size warnings are the file manager's own bug;
            // we can only keep them out of our console, not fix them.)
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["NO_AT_BRIDGE"] = "1";
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        var process = Process.Start(psi);
        if (process is not null)
        {
            // Drain both streams to nowhere so the (long-lived) child never blocks on a full pipe.
            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
        }
    }

    public void ShellOpen(string target)
    {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);
}
