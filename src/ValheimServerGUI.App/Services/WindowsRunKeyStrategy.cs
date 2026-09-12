using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Windows run-on-login via the per-user HKCU <c>Run</c> key (the HKLM tier is dropped — see §16: it
/// needed admin and silently fell back to HKCU anyway). Idempotent, and repairs a stale value that points
/// at the wrong executable (E53).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRunKeyStrategy : IStartupStrategy
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _valueName;
    private readonly string _command;

    public WindowsRunKeyStrategy(string valueName, string executablePath)
    {
        _valueName = valueName;
        // Quote the path so a space in the install directory does not split the command.
        _command = $"\"{executablePath}\"";
    }

    public bool Apply(bool runOnStartup)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (key is null) return false;

        var current = key.GetValue(_valueName) as string;

        if (!runOnStartup)
        {
            if (current is null) return false;
            key.DeleteValue(_valueName, throwOnMissingValue: false);
            return true;
        }

        if (string.Equals(current, _command, StringComparison.OrdinalIgnoreCase))
            return false; // already correct — idempotent no-op

        key.SetValue(_valueName, _command);
        return true;
    }
}
