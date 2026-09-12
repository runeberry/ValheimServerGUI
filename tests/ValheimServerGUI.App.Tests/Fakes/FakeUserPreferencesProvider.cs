using System;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>In-memory <see cref="IUserPreferencesProvider"/> — no disk, records save count.</summary>
internal sealed class FakeUserPreferencesProvider : IUserPreferencesProvider
{
    private UserPreferences _prefs;

    public FakeUserPreferencesProvider(UserPreferences? prefs = null) => _prefs = prefs ?? new UserPreferences();

    public int SaveCount { get; private set; }

    public event EventHandler<UserPreferences>? PreferencesSaved;

    public UserPreferences LoadPreferences() => _prefs;

    public void SavePreferences(UserPreferences preferences)
    {
        _prefs = preferences;
        SaveCount++;
        PreferencesSaved?.Invoke(this, preferences);
    }
}
