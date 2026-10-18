namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Asks the user a question through the UI. The implementation is supplied by the desktop app; tests
    /// inject a recording/no-op prompt.
    /// </summary>
    public interface IUserPrompt
    {
        /// <summary>Asks the user a yes/no question. Returns true if the user chose "yes".</summary>
        bool Confirm(string message, string title);
    }
}
