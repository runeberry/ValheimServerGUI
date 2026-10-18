namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Reads and applies the app's run-on-startup registration (Windows: the Run registry key; Linux: an
    /// autostart <c>.desktop</c> entry). The OS is the only record of the setting, so a change made outside the
    /// app shows up here. The OS-specific implementation is supplied by the desktop app, which knows the
    /// product name and executable path.
    /// </summary>
    public interface IStartupManager
    {
        /// <summary>True if the OS will launch this copy of the app at login.</summary>
        bool IsStartupEnabled();

        /// <summary>
        /// Brings the run-on-startup registration in line with <paramref name="runOnStartup"/>.
        /// Returns true if a change was actually made.
        /// </summary>
        bool ApplyStartupSetting(bool runOnStartup);
    }
}
