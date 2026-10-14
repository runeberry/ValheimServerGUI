using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;

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
/// The user's exit decision when they close the last window (§2.4 "safe shutdowns"; an OS shutdown never asks, see
/// <c>App.OnShutdownRequested</c>), as an <em>aggregate</em> over every running server:
/// servers are shared app-wide and outlive individual windows, so the save-flush guard applies when the app exits,
/// not per-window close. Side-effect-free (the caller performs the actual Stop / Shutdown) so every branch is
/// unit-testable with a recording confirm.
/// </summary>
public static class CloseDecider
{
    /// <param name="confirm">Asks the user a yes/no question; true means yes.</param>
    public static async Task<CloseDecision> DecideAsync(
        IReadOnlyCollection<ServerStatus> statuses, Func<string, Task<bool>> confirm)
    {
        var anyActive = statuses.Any(s => s is ServerStatus.Starting or ServerStatus.Running);
        var anyStopping = statuses.Any(s => s == ServerStatus.Stopping);

        // Nothing running or shutting down: nothing to flush.
        if (!anyActive && !anyStopping)
            return CloseDecision.Proceed;

        if (anyActive)
            return await confirm(Strings.Prompt_ExitWhileRunning) ? CloseDecision.StopThenClose : CloseDecision.Cancel;

        // Only server(s) mid-shutdown remain.
        return await confirm(Strings.Prompt_ExitWhileStopping) ? CloseDecision.Proceed : CloseDecision.Cancel;
    }
}
