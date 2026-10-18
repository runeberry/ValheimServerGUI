using System;
using System.IO;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Linux run-on-login via an XDG autostart <c>.desktop</c> entry in <c>~/.config/autostart/</c>. Enabling
/// writes the entry; disabling removes it. Enabled means the entry is exactly the one this executable writes, so
/// an external edit (such as <c>Hidden=true</c>) or a stale exec path reads as disabled and re-enabling rewrites it.
/// </summary>
public sealed class LinuxAutostartStrategy : IStartupStrategy
{
    private readonly string _autostartDir;
    private readonly string _execPath;
    private readonly string _appName;
    private readonly string _fileName;

    public LinuxAutostartStrategy(string autostartDir, string execPath, string appName, string fileBaseName)
    {
        _autostartDir = autostartDir;
        _execPath = execPath;
        _appName = appName;
        _fileName = fileBaseName + ".desktop";
    }

    private string FilePath => Path.Combine(_autostartDir, _fileName);

    public bool IsEnabled() => File.Exists(FilePath) && File.ReadAllText(FilePath) == BuildEntry();

    public bool Apply(bool runOnStartup)
    {
        var path = FilePath;

        if (!runOnStartup)
        {
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        if (IsEnabled())
            return false; // already correct — idempotent no-op

        Directory.CreateDirectory(_autostartDir);
        File.WriteAllText(path, BuildEntry());
        return true;
    }

    private string BuildEntry() =>
        "[Desktop Entry]\n" +
        "Type=Application\n" +
        $"Name={_appName}\n" +
        $"Exec=\"{_execPath}\"\n" +
        "Terminal=false\n" +
        "X-GNOME-Autostart-enabled=true\n";
}
