using System.Collections.Generic;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Tests.Fakes
{
    /// <summary>A per-test <see cref="IPlayerListBaselineStore"/> with no file behind it.</summary>
    public class InMemoryBaselineStore : IPlayerListBaselineStore
    {
        private readonly Dictionary<string, PlayerListBaseline> _baselines = new();

        public PlayerListBaseline? Get(string saveDataFolder)
            => _baselines.TryGetValue(saveDataFolder, out var baseline) ? baseline : null;

        public void Save(string saveDataFolder, PlayerListBaseline baseline) => _baselines[saveDataFolder] = baseline;
    }
}
