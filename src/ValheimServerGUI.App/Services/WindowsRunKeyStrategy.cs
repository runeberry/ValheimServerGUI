using System;

namespace ValheimServerGUI.App.Services;

/// <summary>
/// Windows run-on-login via a value in the per-user <c>Run</c> registry key. Enabled means Windows will launch
/// this executable at login: the value points here and the user has not disabled it in Task Manager or
/// Settings. Idempotent, and repairs a stale value that points at the wrong executable.
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

    public bool IsEnabled() =>
        string.Equals(_runKey.GetValue(_valueName), _command, StringComparison.OrdinalIgnoreCase)
        && !_runKey.IsDisabledByUser(_valueName);

    public bool Apply(bool runOnStartup)
    {
        if (!runOnStartup)
        {
            var existed = _runKey.GetValue(_valueName) is not null || _runKey.IsDisabledByUser(_valueName);
            _runKey.DeleteValue(_valueName);
            _runKey.ClearUserDisable(_valueName);
            return existed;
        }

        if (IsEnabled())
            return false; // already correct — idempotent no-op

        _runKey.SetValue(_valueName, _command);
        // A user disable outlives the Run value, so it is cleared or the entry would stay off.
        _runKey.ClearUserDisable(_valueName);
        return true;
    }
}
