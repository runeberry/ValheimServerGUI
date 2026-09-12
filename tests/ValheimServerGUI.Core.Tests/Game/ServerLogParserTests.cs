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
    /// The seam payoff: the v2.4 ValheimServerTests scenarios, migrated to drive the parser directly
    /// via <see cref="ServerLogParser.ProcessLine"/> with a real <see cref="PlayerDataRepository"/> over
    /// an in-memory file provider. Zero process/exe/filesystem dependency -- these run on Linux with no
    /// Valheim install, which the old Windows-path boot hack could not. Covers the regex table, the
    /// server-connected/world-saved/invite-code signals, and the join->online correlation heuristic
    /// (E21-E28), including a negative ZDOID.
    /// </summary>
    public class ServerLogParserTests
    {
        private const string MessageJoining = "Got connection SteamID {0}";
        private const string MessageJoiningCrossplay = "PlayFab socket with remote ID 123456 received local Platform ID {0}_{1}";
        private const string MessageOnline = "Got character ZDOID from {0} : {1}:1";
        private const string MessageWrongPassword = "Peer {0} has wrong password";
        private const string MessageOffline = "Closing socket {0}";
        private const string MessageOfflineCrossplay = "Destroying abandoned non persistent zdo {0}:1";

        private readonly PlayerDataRepository _repo;
        private readonly ServerLogParser _parser;

        public ServerLogParserTests()
        {
            var fileProvider = new MockDataFileProvider();
            ILogger serilog = new LoggerConfiguration().CreateLogger();
            var context = new DataFileRepositoryContext(fileProvider, serilog);
            var runeberry = new FakeRuneberryApiClient();
            var resolver = new LinuxValheimPathResolver("/tmp/vsg-test-home", xdgDataHome: null);

            _repo = new PlayerDataRepository(context, runeberry, resolver);
            _parser = new ServerLogParser(_repo, new FakeApplicationLogger());
        }

        private void Process(string template, params object[] args) => _parser.ProcessLine(string.Format(template, args));

        private static void AssertMatch(string platform, string playerId, PlayerStatus status, PlayerInfo? actual,
            string? zdoid = null, string? lastStatusCharacter = null)
        {
            Assert.NotNull(actual);
            Assert.Equal(platform, actual!.Platform);
            Assert.Equal(playerId, actual.PlayerId);
            Assert.Equal(status, actual.PlayerStatus);
            if (zdoid != null) Assert.Equal(zdoid, actual.ZdoId);
            if (lastStatusCharacter != null) Assert.Equal(lastStatusCharacter, actual.LastStatusCharacter);
        }

        [Fact]
        public void Connected_RaisesServerConnectedSignal()
        {
            var raised = 0;
            _parser.ServerConnected += (_, _) => raised++;

            _parser.ProcessLine("Game server connected");

            Assert.Equal(1, raised);
        }

        [Fact]
        public void PlayerJoining_Steam_IsRecorded()
        {
            PlayerInfo? eventPlayer = null;
            _repo.PlayerStatusChanged += (_, p) => eventPlayer = p;

            Process(MessageJoining, "1234");

            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Joining, eventPlayer);
            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Joining, Assert.Single(_repo.Data));
        }

        [Theory]
        [InlineData(PlayerPlatforms.Steam, "1234")]
        [InlineData(PlayerPlatforms.Xbox, "5678")]
        public void PlayerJoiningCrossplay_IsRecorded(string platform, string playerId)
        {
            PlayerInfo? eventPlayer = null;
            _repo.PlayerStatusChanged += (_, p) => eventPlayer = p;

            Process(MessageJoiningCrossplay, platform, playerId);

            AssertMatch(platform, playerId, PlayerStatus.Joining, eventPlayer);
            AssertMatch(platform, playerId, PlayerStatus.Joining, Assert.Single(_repo.Data));
        }

        [Fact]
        public void UnknownCrossplayPlatform_IsNotRecorded()
        {
            // E21: a platform the app doesn't know is ignored.
            Process(MessageJoiningCrossplay, "PlayStation", "9999");
            Assert.Empty(_repo.Data);
        }

        [Fact]
        public void WrongPassword_AfterJoining_MarksLeaving()
        {
            Process(MessageJoining, "1234");
            Process(MessageWrongPassword, "1234");

            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Leaving, Assert.Single(_repo.Data));
        }

        [Fact]
        public void ClosingSocket_AfterJoining_MarksOffline()
        {
            Process(MessageJoining, "1234");
            Process(MessageOffline, "1234");

            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Offline, Assert.Single(_repo.Data));
        }

        [Fact]
        public void OfflineByZdoId_Crossplay_MarksOffline()
        {
            // E27/E28: crossplay disconnect is matched by (negative) ZDOID via the Or-query.
            Process(MessageJoiningCrossplay, PlayerPlatforms.Xbox, "5678");
            Process(MessageOnline, "Crossy", "-4242");
            Process(MessageOfflineCrossplay, "-4242");

            AssertMatch(PlayerPlatforms.Xbox, "5678", PlayerStatus.Offline, Assert.Single(_repo.Data));
        }

        [Fact]
        public void SingleJoiner_GoesOnline_ConfidentMatch_NegativeZdoid()
        {
            Process(MessageJoining, "1234");
            Process(MessageOnline, "Broheim", "-56789123");

            var player = Assert.Single(_repo.Data);
            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Online, player,
                zdoid: "-56789123", lastStatusCharacter: "Broheim");
            var character = Assert.Single(player.Characters!);
            Assert.Equal("Broheim", character.CharacterName);
            Assert.True(character.MatchConfident);
        }

        [Fact]
        public void MultipleJoinersAtOnce_AssignedEarliestFirst_LowConfidence()
        {
            // E24: several join with no known names; each character online is assigned to the
            // earliest still-joining player, flagged low-confidence.
            var ids = new[] { "123", "456", "789" };
            Process(MessageJoining, ids[0]);
            Process(MessageJoining, ids[1]);
            Process(MessageJoining, ids[2]);
            Assert.Equal(3, _repo.Data.Count());

            PlayerInfo? eventPlayer = null;
            _repo.PlayerStatusChanged += (_, p) => eventPlayer = p;

            Process(MessageOnline, "CharOne", "-432");
            AssertMatch(PlayerPlatforms.Steam, ids[0], PlayerStatus.Online, eventPlayer, lastStatusCharacter: "CharOne");
            Assert.False(_repo.FindById($"{PlayerPlatforms.Steam}:{ids[0]}")!.Characters!.Single().MatchConfident);

            Process(MessageOnline, "CharTwo", "5834");
            AssertMatch(PlayerPlatforms.Steam, ids[1], PlayerStatus.Online, eventPlayer, lastStatusCharacter: "CharTwo");

            Process(MessageOnline, "CharThree", "146131");
            AssertMatch(PlayerPlatforms.Steam, ids[2], PlayerStatus.Online, eventPlayer, lastStatusCharacter: "CharThree");
        }

        [Fact]
        public void SameSteamId_RejoinsUnderNewCharacter_TracksBothCharacters()
        {
            // E26: a rejoin reuses the most-recently-offline record (no dup); E25: a second character
            // is added and lastStatusCharacter updated.
            Process(MessageJoining, "1234");
            Process(MessageOnline, "CharacterOne", "234");
            Process(MessageOffline, "1234");

            Process(MessageJoining, "1234");
            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Joining, Assert.Single(_repo.Data));

            Process(MessageOnline, "CharacterTwo", "567");

            var player = Assert.Single(_repo.Data);
            AssertMatch(PlayerPlatforms.Steam, "1234", PlayerStatus.Online, player,
                zdoid: "567", lastStatusCharacter: "CharacterTwo");
            Assert.Equal(2, player.Characters!.Count);
        }

        [Fact]
        public void WorldSaved_RaisesEventWithDuration()
        {
            decimal? saved = null;
            _parser.WorldSaved += (_, ms) => saved = ms;

            _parser.ProcessLine("World saved ( 123.45ms )");

            Assert.Equal(123.45m, saved);
        }

        [Fact]
        public void InviteCode_RaisesEvent()
        {
            string? code = null;
            _parser.InviteCodeReady += (_, c) => code = c;

            _parser.ProcessLine("Session \"Test Server\" with join code 987654 for something");

            Assert.Equal("987654", code);
        }
    }
}
