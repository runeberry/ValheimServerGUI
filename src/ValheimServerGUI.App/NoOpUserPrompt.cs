using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App;

/// <summary>
/// Placeholder <see cref="IUserPrompt"/> used until Wave 1 supplies the real Window-backed prompt.
/// Declines every confirmation (so, for example, the exception handler skips the crash-report upload).
/// </summary>
internal sealed class NoOpUserPrompt : IUserPrompt
{
    public bool Confirm(string message, string title) => false;
}
