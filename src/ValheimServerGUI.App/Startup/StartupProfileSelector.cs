using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Startup;

/// <summary>One window to open at startup: the profile to load, and whether to auto-start its server.</summary>
public sealed record StartupWindowPlan(ServerPreferences Profile, bool AutoStart);

/// <summary>
/// The outcome of startup profile selection. <see cref="CreateDefault"/> signals the orchestrator to
/// create + save a fresh "Default" profile (no profiles existed) and open a single window for it.
/// </summary>
public sealed record StartupSelection(IReadOnlyList<StartupWindowPlan> Windows, bool CreateDefault);

/// <summary>
/// Pure port of <c>SplashForm.PrepareMainWindows</c> (§2.3). Selection order:
/// <list type="number">
/// <item>Any profile flagged <c>AutoStart</c> → one window per auto-start profile, each started.</item>
/// <item>Else → a single window for the last-active profile (§16.2), falling back to the most-recently
/// saved (<c>LastSaved</c>), not started.</item>
/// <item>Else (no profiles) → signal <see cref="StartupSelection.CreateDefault"/>.</item>
/// </list>
/// </summary>
public static class StartupProfileSelector
{
    public static StartupSelection Select(IReadOnlyList<ServerPreferences> profiles, string? lastActiveProfile)
    {
        if (profiles.Count == 0)
            return new StartupSelection(new List<StartupWindowPlan>(), CreateDefault: true);

        var autoStart = profiles.Where(p => p.AutoStart).ToList();
        if (autoStart.Count > 0)
        {
            var plans = autoStart
                .OrderBy(p => p.ProfileName)
                .Select(p => new StartupWindowPlan(p, AutoStart: true))
                .ToList();
            return new StartupSelection(plans, CreateDefault: false);
        }

        var chosen = FindByName(profiles, lastActiveProfile)
                     ?? profiles.OrderByDescending(p => p.LastSaved).First();

        return new StartupSelection(
            new[] { new StartupWindowPlan(chosen, AutoStart: false) },
            CreateDefault: false);
    }

    private static ServerPreferences? FindByName(IReadOnlyList<ServerPreferences> profiles, string? name)
        => string.IsNullOrWhiteSpace(name) ? null : profiles.FirstOrDefault(p => p.ProfileName == name);
}
