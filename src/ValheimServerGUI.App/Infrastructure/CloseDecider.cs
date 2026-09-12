using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>What the window should do about a close request (§2.4).</summary>
public enum CloseDecision
{
    /// <summary>Let the close proceed immediately.</summary>
    Proceed,

    /// <summary>Cancel the close and keep the window open.</summary>
    Cancel,

    /// <summary>Cancel the immediate close, gracefully stop the server, and close once it reports Stopped.</summary>
    StopThenClose,
}

/// <summary>
/// Pure port of the v2.4 "safe shutdowns" close logic (§2.4). Kept side-effect-free (the caller performs
/// the actual Stop / Close) so every branch is unit-testable with a recording <see cref="IUserPrompt"/>.
/// </summary>
public static class CloseDecider
{
    public static CloseDecision Decide(ServerStatus status, bool isOsShutdown, IUserPrompt prompt, string title)
    {
        if (status == ServerStatus.Stopped)
            return CloseDecision.Proceed;

        // OS shutdown / logoff: never prompt — flush the save by stopping gracefully, then close.
        if (isOsShutdown)
            return CloseDecision.StopThenClose;

        if (status == ServerStatus.Stopping)
        {
            return prompt.Confirm(
                "The server is still shutting down. Close anyway? Unsaved world data may be lost.", title)
                ? CloseDecision.Proceed
                : CloseDecision.Cancel;
        }

        // Starting / Running.
        return prompt.Confirm(
            "The server is still running. Stop the server and close this window?", title)
            ? CloseDecision.StopThenClose
            : CloseDecision.Cancel;
    }
}
