namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Applies the "run on startup" user preference (Windows: the Run registry key; Linux: an autostart
    /// <c>.desktop</c> entry). The OS-specific implementation is supplied by the desktop app, which knows the
    /// product name and executable path.
    /// </summary>
    public interface IStartupManager
    {
        /// <summary>
        /// Brings the run-on-startup registration in line with <paramref name="runOnStartup"/>.
        /// Returns true if a change was actually made.
        /// </summary>
        bool ApplyStartupSetting(bool runOnStartup);
    }
}
