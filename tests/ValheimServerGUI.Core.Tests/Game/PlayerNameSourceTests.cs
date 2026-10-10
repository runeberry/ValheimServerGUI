using System.Collections.Generic;
using System.Linq;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// A known player's platform name comes from the world save's player history (the name the client reported for
    /// itself). Only players VSG already knows are named.
    /// </summary>
    public class PlayerNameSourceTests
    {
        private readonly PlayerDataRepository _repo;

        public PlayerNameSourceTests()
        {
            ILogger serilog = new LoggerConfiguration().CreateLogger();
            var context = new DataFileRepositoryContext(new MockDataFileProvider(), serilog);
            var resolver = new LinuxValheimPathResolver("/tmp/vsg-test-home", xdgDataHome: null);
            _repo = new PlayerDataRepository(context, resolver);
        }

        private PlayerInfo Known(string platform, string playerId, string? name = null)
        {
            var player = new PlayerInfo { Platform = platform, PlayerId = playerId, PlayerName = name };
            _repo.Upsert(player);
            return player;
        }

        private static WorldPlayerHistoryEntry Entry(string id, string name) => new(id, name, name, "");

        [Fact]
        public void WorldHistory_NamesKnownSteamAndXboxPlayers()
        {
            var steam = Known(PlayerPlatforms.Steam, "76561198000000001");
            var xbox = Known(PlayerPlatforms.Xbox, "2533274900000001");

            _repo.ApplyWorldPlayerHistory(new[]
            {
                Entry("Steam_76561198000000001", "Viking"),
                Entry("Xbox_2533274900000001", "Shieldmaiden1"),
            });

            Assert.Equal("Viking", steam.PlayerName);
            Assert.Equal("Shieldmaiden1", xbox.PlayerName);
        }

        // A persona or gamertag change reaches VSG on the player's next join + save.
        [Fact]
        public void WorldHistory_ReplacesAStaleName()
        {
            var player = Known(PlayerPlatforms.Steam, "76561198000000001", name: "OldPersona");

            _repo.ApplyWorldPlayerHistory(new[] { Entry("Steam_76561198000000001", "NewPersona") });

            Assert.Equal("NewPersona", player.PlayerName);
        }

        [Fact]
        public void WorldHistory_EmptyName_KeepsTheExistingName()
        {
            var player = Known(PlayerPlatforms.Xbox, "2533274900000001", name: "Shieldmaiden1");

            _repo.ApplyWorldPlayerHistory(new[] { Entry("Xbox_2533274900000001", "") });

            Assert.Equal("Shieldmaiden1", player.PlayerName);
        }

        [Fact]
        public void WorldHistory_DoesNotCreateRecordsForUnknownPlayers()
        {
            _repo.ApplyWorldPlayerHistory(new[]
            {
                Entry("Steam_76561198000000001", "Viking"),
                Entry("Unknown_123", "Stranger"),
            });

            Assert.Empty(_repo.Data);
        }

        // The UI refreshes player rows off PlayerStatusChanged, so a name change must raise it, and only a change.
        [Fact]
        public void WorldHistory_RaisesPlayerStatusChanged_OnlyWhenTheNameChanges()
        {
            Known(PlayerPlatforms.Steam, "76561198000000001");
            var raised = new List<string?>();
            _repo.PlayerStatusChanged += (_, p) => raised.Add(p.PlayerName);

            _repo.ApplyWorldPlayerHistory(new[] { Entry("Steam_76561198000000001", "Viking") });
            _repo.ApplyWorldPlayerHistory(new[] { Entry("Steam_76561198000000001", "Viking") });

            Assert.Equal(new[] { "Viking" }, raised);
        }
    }
}
