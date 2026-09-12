using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>In-memory <see cref="IServerPreferencesProvider"/> mirroring the real provider's semantics.</summary>
internal sealed class FakeServerPreferencesProvider : IServerPreferencesProvider
{
    private readonly List<ServerPreferences> _profiles;

    public FakeServerPreferencesProvider(IEnumerable<ServerPreferences>? profiles = null)
        => _profiles = profiles?.ToList() ?? new List<ServerPreferences>();

    public event EventHandler<List<ServerPreferences>>? PreferencesSaved;

    public ServerPreferences? LoadPreferences(string profileName)
        => _profiles.FirstOrDefault(p => p.ProfileName == profileName);

    public IEnumerable<ServerPreferences> LoadPreferences() => _profiles.ToList();

    public void SavePreferences(ServerPreferences preferences)
    {
        _profiles.RemoveAll(p => p.ProfileName == preferences.ProfileName);
        preferences.LastSaved = DateTime.UtcNow;
        _profiles.Add(preferences);
        PreferencesSaved?.Invoke(this, _profiles.ToList());
    }

    public void RemovePreferences(string profileName)
    {
        _profiles.RemoveAll(p => p.ProfileName == profileName);
        PreferencesSaved?.Invoke(this, _profiles.ToList());
    }
}
