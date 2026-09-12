namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Applies the "run on startup" user preference (Windows: the Run registry key; Linux: an autostart
    /// <c>.desktop</c> entry). Bucket B: the interface lives in Core as the contract, but the OS-specific
    /// implementation is supplied by the Phase 2 desktop shell, which knows the product name and
    /// executable path.
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
