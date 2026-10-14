namespace ValheimServerGUI.App.Services;

/// <summary>
/// The OS process/filesystem boundary the shell launcher sits on. Injected so the per-OS launch commands
/// can be asserted in tests without actually spawning a process or touching the real filesystem.
/// </summary>
public interface ISystemShell
{
    /// <summary>Launches an executable with the given arguments (no shell parsing; args passed verbatim).</summary>
    void Start(string fileName, params string[] arguments);

    /// <summary>Launches a path/URL through the OS's default handler (Windows shell-execute).</summary>
    void ShellOpen(string target);

    bool DirectoryExists(string path);

    bool FileExists(string path);
}
