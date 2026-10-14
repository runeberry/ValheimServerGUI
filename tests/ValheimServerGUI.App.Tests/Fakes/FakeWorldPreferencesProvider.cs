using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Tests.Fakes;

internal sealed class FakeWorldPreferencesProvider : IWorldPreferencesProvider
{
    private readonly Dictionary<string, WorldPreferences> _worlds = new();

    public event EventHandler<List<WorldPreferences>>? PreferencesSaved;

    public WorldPreferences? LoadPreferences(string worldName)
        => _worlds.TryGetValue(worldName, out var p) ? p : null;

    public IEnumerable<WorldPreferences> LoadPreferences() => _worlds.Values.ToList();

    public void SavePreferences(WorldPreferences preferences)
    {
        _worlds[preferences.WorldName!] = preferences;
        PreferencesSaved?.Invoke(this, _worlds.Values.ToList());
    }

    public void RemovePreferences(string worldName)
    {
        _worlds.Remove(worldName);
        PreferencesSaved?.Invoke(this, _worlds.Values.ToList());
    }
}
