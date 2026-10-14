namespace ValheimServerGUI.Tools
{
    /// <summary>
    /// Asks the user a question through the UI (formerly the <c>MessageBox</c> half of the exception
    /// handler). Bucket B: the interface lives in Core as the contract; the implementation is supplied
    /// by the Phase 2 desktop shell. Tests inject a recording/no-op prompt.
    /// </summary>
    public interface IUserPrompt
    {
        /// <summary>Asks the user a yes/no question. Returns true if the user chose "yes".</summary>
        bool Confirm(string message, string title);
    }
}
