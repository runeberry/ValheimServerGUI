using System;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Windows run-on-login via a value in the per-user <c>Run</c> registry key. Idempotent, and repairs a stale
/// value that points at the wrong executable.
/// </summary>
public sealed class WindowsRunKeyStrategy : IStartupStrategy
{
    private readonly IRunKey _runKey;
    private readonly string _valueName;
    private readonly string _command;

    public WindowsRunKeyStrategy(IRunKey runKey, string valueName, string executablePath)
    {
        _runKey = runKey;
        _valueName = valueName;
        // Quote the path so a space in the install directory does not split the command.
        _command = $"\"{executablePath}\"";
    }

    public bool Apply(bool runOnStartup)
    {
        var current = _runKey.GetValue(_valueName);

        if (!runOnStartup)
        {
            if (current is null) return false;
            _runKey.DeleteValue(_valueName);
            return true;
        }

        if (string.Equals(current, _command, StringComparison.OrdinalIgnoreCase))
            return false; // already correct — idempotent no-op

        _runKey.SetValue(_valueName, _command);
        return true;
    }
}
