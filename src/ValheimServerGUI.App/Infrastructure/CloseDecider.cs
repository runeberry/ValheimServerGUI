using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>What the app should do about a shutdown request (§2.4).</summary>
public enum CloseDecision
{
    /// <summary>Let the shutdown proceed immediately.</summary>
    Proceed,

    /// <summary>Cancel the shutdown and keep the app running.</summary>
    Cancel,

    /// <summary>Cancel the immediate shutdown, gracefully stop the servers, and shut down once they report Stopped.</summary>
    StopThenClose,
}

/// <summary>
/// Pure port of the v2.4 "safe shutdowns" logic (§2.4), now an <em>aggregate</em> over every running server:
/// servers are shared app-wide and outlive individual windows, so the save-flush guard applies at app
/// shutdown, not per-window close. Side-effect-free (the caller performs the actual Stop / Shutdown) so every
/// branch is unit-testable with a recording <see cref="IUserPrompt"/>.
/// </summary>
public static class CloseDecider
{
    public static CloseDecision Decide(
        IReadOnlyCollection<ServerStatus> statuses, bool isOsShutdown, IUserPrompt prompt, string title)
    {
        var anyActive = statuses.Any(s => s is ServerStatus.Starting or ServerStatus.Running);
        var anyStopping = statuses.Any(s => s == ServerStatus.Stopping);

        // Nothing running or shutting down: nothing to flush.
        if (!anyActive && !anyStopping)
            return CloseDecision.Proceed;

        // OS shutdown / logoff: never prompt — flush the saves by stopping gracefully, then let it proceed.
        if (isOsShutdown)
            return CloseDecision.StopThenClose;

        if (anyActive)
        {
            return prompt.Confirm(
                "A Valheim server is still running. Do you want to stop it and exit?", title)
                ? CloseDecision.StopThenClose
                : CloseDecision.Cancel;
        }

        // Only server(s) mid-shutdown remain.
        return prompt.Confirm(
            "A Valheim server is currently shutting down. Exit anyway?\n" +
            "This could result in a loss of save data!", title)
            ? CloseDecision.Proceed
            : CloseDecision.Cancel;
    }
}
