namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Opens folders and web addresses in the OS shell (Windows: <c>explorer.exe</c>; Linux:
    /// <c>xdg-open</c>). The OS-specific implementation is supplied by the desktop app.
    /// </summary>
    public interface IShellLauncher
    {
        /// <summary>Opens the given directory (or the containing directory of a file) in the file manager.</summary>
        void OpenDirectory(string path);

        /// <summary>Opens the given web address in the default browser.</summary>
        void OpenWebAddress(string url);
    }
}
