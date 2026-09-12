namespace ValheimServerGUI.App.Services;

/// <summary>
/// The per-OS mechanism behind <see cref="ValheimServerGUI.Tools.IStartupManager"/>: Windows uses the
/// HKCU Run key, Linux an XDG autostart <c>.desktop</c> entry. Split out so each strategy is unit-tested
/// against an injected boundary (a temp autostart dir on Linux; a Win-guarded registry test on Windows).
/// </summary>
public interface IStartupStrategy
{
    /// <summary>
    /// Brings the run-on-startup registration in line with <paramref name="runOnStartup"/>, including
    /// repairing a stale entry that points at the wrong executable. Returns true if a change was made.
    /// </summary>
    bool Apply(bool runOnStartup);
}
