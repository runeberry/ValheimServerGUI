using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.Startup;

// §2.3 PrepareMainWindows selection matrix.
public class StartupProfileSelectorTests
{
    private static ServerPreferences Profile(string name, bool autoStart = false, int savedDaysAgo = 0)
        => new()
        {
            ProfileName = name,
            AutoStart = autoStart,
            LastSaved = DateTime.UtcNow.AddDays(-savedDaysAgo),
        };

    [Fact]
    public void No_profiles_signals_create_default()
    {
        var selection = StartupProfileSelector.Select(Array.Empty<ServerPreferences>(), lastActiveProfile: null);

        Assert.True(selection.CreateDefault);
        Assert.Empty(selection.Windows);
    }

    [Fact]
    public void Autostart_opens_one_window_per_autostart_profile_all_started()
    {
        var profiles = new[]
        {
            Profile("Alpha", autoStart: true),
            Profile("Bravo", autoStart: false),
            Profile("Charlie", autoStart: true),
        };

        var selection = StartupProfileSelector.Select(profiles, lastActiveProfile: null);

        Assert.False(selection.CreateDefault);
        Assert.Equal(2, selection.Windows.Count);
        Assert.All(selection.Windows, w => Assert.True(w.AutoStart));
        Assert.Equal(new[] { "Alpha", "Charlie" }, selection.Windows.Select(w => w.Profile.ProfileName));
    }

    [Fact]
    public void No_autostart_opens_most_recently_saved_not_started()
    {
        var profiles = new[]
        {
            Profile("Old", savedDaysAgo: 5),
            Profile("Newest", savedDaysAgo: 1),
            Profile("Middle", savedDaysAgo: 3),
        };

        var selection = StartupProfileSelector.Select(profiles, lastActiveProfile: null);

        var window = Assert.Single(selection.Windows);
        Assert.Equal("Newest", window.Profile.ProfileName);
        Assert.False(window.AutoStart);
    }

    [Fact]
    public void No_autostart_prefers_last_active_over_most_recent()
    {
        var profiles = new[]
        {
            Profile("Newest", savedDaysAgo: 1),
            Profile("Preferred", savedDaysAgo: 9),
        };

        var selection = StartupProfileSelector.Select(profiles, lastActiveProfile: "Preferred");

        Assert.Equal("Preferred", Assert.Single(selection.Windows).Profile.ProfileName);
    }

    [Fact]
    public void Last_active_that_no_longer_exists_falls_back_to_most_recent()
    {
        var profiles = new[] { Profile("A", savedDaysAgo: 2), Profile("B", savedDaysAgo: 1) };

        var selection = StartupProfileSelector.Select(profiles, lastActiveProfile: "Ghost");

        Assert.Equal("B", Assert.Single(selection.Windows).Profile.ProfileName);
    }

    [Fact]
    public void Autostart_takes_precedence_over_last_active()
    {
        var profiles = new[]
        {
            Profile("Auto", autoStart: true),
            Profile("Preferred"),
        };

        var selection = StartupProfileSelector.Select(profiles, lastActiveProfile: "Preferred");

        Assert.Equal("Auto", Assert.Single(selection.Windows).Profile.ProfileName);
    }
}
