using System;
using System.IO;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// Reading the player history out of a world's <c>.fwl2</c> save: the real game-written files, layouts the real
    /// files don't cover, hostile input (which must fail as <see cref="InvalidDataException"/>, never crash or
    /// over-allocate), and choosing the newest committed save in a world folder.
    /// </summary>
    public class WorldPlayerHistoryTests : IDisposable
    {
        private readonly DirectoryInfo WorldFolder =
            new(Path.Join(Path.GetTempPath(), "vsg_world_" + Guid.NewGuid().ToString("N")));

        public void Dispose()
        {
            if (WorldFolder.Exists) WorldFolder.Delete(recursive: true);
        }

        private static WorldPlayerHistoryEntry Entry(string id, string name, string playFabId = "")
            => new(id, name, name, playFabId);

        private static System.Collections.Generic.IReadOnlyList<WorldPlayerHistoryEntry> Read(byte[] bytes)
            => WorldPlayerHistory.Read(new MemoryStream(bytes));

        #region Real saves

        [Fact]
        public void Read_RealSave_ReturnsSteamAndXboxPlayers()
        {
            var history = Read(WorldSaveFixtures.ReadFixture(WorldSaveFixtures.TwoPlatforms));

            Assert.Equal(
                new[]
                {
                    new WorldPlayerHistoryEntry("Steam_76561198000000001", "Viking", "Viking", "AAAA000000000001"),
                    new WorldPlayerHistoryEntry("Xbox_2533274900000001", "Shieldmaiden1", "Shieldmaiden1", "BBBB000000000002"),
                },
                history);
        }

        [Fact]
        public void Read_RealSaveWithGlobalKey_SkipsTheKey()
        {
            var history = Read(WorldSaveFixtures.ReadFixture(WorldSaveFixtures.WithGlobalKey));

            Assert.Equal(
                new[] { new WorldPlayerHistoryEntry("Steam_76561198000000001", "Viking", "Viking", "AAAA000000000001") },
                history);
        }

        #endregion

        #region Layouts

        [Fact]
        public void Read_SeveralGlobalKeysAndUnusualNames_Parses()
        {
            var entries = new[]
            {
                new WorldPlayerHistoryEntry("Steam_76561198000000002", "Odin (the) Allfather, Esq.", "Odin (the) Allfather, Esq.", ""),
                new WorldPlayerHistoryEntry("PlayStation_1234567890", "Ragnhild 🐉", "Ragnhild 🐉", "CCCC000000000003"),
                new WorldPlayerHistoryEntry("Xbox_2533274900000002", "", "", "DDDD000000000004"),
                new WorldPlayerHistoryEntry("Steam_76561198000000003", "Viking", "Viking#2", ""),
            };

            var history = Read(WorldSaveFixtures.Build(entries, globalKeys: new[] { "nomap", "playerdamage 50", "" }));

            Assert.Equal(entries, history);
        }

        [Fact]
        public void Read_EmptyHistory_ReturnsEmpty()
        {
            Assert.Empty(Read(WorldSaveFixtures.Build(Array.Empty<WorldPlayerHistoryEntry>())));
        }

        // Worlds saved before Version.World.DeepNorth carry no history; that is not an error.
        [Fact]
        public void Read_VersionBeforePlayerHistory_ReturnsEmpty()
        {
            var bytes = WorldSaveFixtures.Build(
                new[] { Entry("Steam_76561198000000001", "Viking") },
                version: WorldPlayerHistory.PlayerHistoryVersion - 1);

            Assert.Empty(Read(bytes));
        }

        [Fact]
        public void Read_NewerVersionWithTheSameLayout_Parses()
        {
            var entries = new[] { Entry("Steam_76561198000000001", "Viking") };

            var history = Read(WorldSaveFixtures.Build(entries, version: WorldPlayerHistory.PlayerHistoryVersion + 1));

            Assert.Equal(entries, history);
        }

        // A future version that inserts a field misaligns everything after it; that must fail, not return garbage.
        [Fact]
        public void Read_NewerVersionWithAnAddedField_Fails()
        {
            var bytes = WorldSaveFixtures.Build(
                new[] { Entry("Steam_76561198000000001", "Viking") },
                version: WorldPlayerHistory.PlayerHistoryVersion + 1,
                extraField: w => w.Write(7));

            Assert.Throws<InvalidDataException>(() => Read(bytes));
        }

        [Fact]
        public void Read_TrailingBytesInPayload_Fails()
        {
            var bytes = WorldSaveFixtures.Build(Array.Empty<WorldPlayerHistoryEntry>());
            var padded = new byte[bytes.Length + 3];
            bytes.CopyTo(padded, 0);
            BitConverter.GetBytes(bytes.Length - 4 + 3).CopyTo(padded, 0);

            Assert.Throws<InvalidDataException>(() => Read(padded));
        }

        #endregion

        #region Hostile input

        [Fact]
        public void Read_EveryTruncation_FailsCleanly()
        {
            var bytes = WorldSaveFixtures.ReadFixture(WorldSaveFixtures.TwoPlatforms);

            for (var length = 0; length < bytes.Length; length++)
            {
                var truncated = bytes.AsSpan(0, length).ToArray();
                Assert.Throws<InvalidDataException>(() => Read(truncated));
            }
        }

        // Any single corrupted byte either still parses or fails as InvalidDataException; nothing else escapes.
        [Fact]
        public void Read_AnySingleCorruptByte_ParsesOrFailsCleanly()
        {
            var bytes = WorldSaveFixtures.ReadFixture(WorldSaveFixtures.TwoPlatforms);

            foreach (var value in new byte[] { 0x00, 0x7F, 0x80, 0xFF })
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    var corrupt = (byte[])bytes.Clone();
                    corrupt[i] = value;
                    try { Read(corrupt); }
                    catch (InvalidDataException) { }
                }
            }
        }

        [Fact]
        public void Read_HugeStringLength_Fails()
        {
            // Payload: version, then a name whose 7-bit length prefix declares int.MaxValue bytes.
            var payload = new byte[] { 41, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0x07 };
            var bytes = new byte[4 + payload.Length];
            BitConverter.GetBytes(payload.Length).CopyTo(bytes, 0);
            payload.CopyTo(bytes, 4);

            Assert.Throws<InvalidDataException>(() => Read(bytes));
        }

        // A string that runs past the payload into bytes after it must not be accepted as part of the save.
        [Fact]
        public void Read_StringOverrunningThePayload_Fails()
        {
            var bytes = WorldSaveFixtures.Build(new[] { Entry("Steam_76561198000000001", "Viking") });
            var padded = new byte[bytes.Length + 16];
            bytes.CopyTo(padded, 0);
            // The last string is the 1-byte-prefixed empty playfab id; declare 10 bytes so it reads past the payload.
            padded[bytes.Length - 1] = 10;

            Assert.Throws<InvalidDataException>(() => Read(padded));
        }

        [Fact]
        public void Read_HugeHistoryCount_Fails()
        {
            var bytes = WorldSaveFixtures.Build(Array.Empty<WorldPlayerHistoryEntry>());
            // The history count is the last int32 in a save with no history.
            BitConverter.GetBytes(int.MaxValue).CopyTo(bytes, bytes.Length - 4);

            Assert.Throws<InvalidDataException>(() => Read(bytes));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        public void Read_BadPayloadLength_Fails(int payloadLength)
        {
            var bytes = WorldSaveFixtures.ReadFixture(WorldSaveFixtures.TwoPlatforms);
            BitConverter.GetBytes(payloadLength).CopyTo(bytes, 0);

            Assert.Throws<InvalidDataException>(() => Read(bytes));
        }

        #endregion

        #region World folder

        private static byte[] SaveNaming(string name) => WorldSaveFixtures.Build(new[] { Entry("Steam_76561198000000001", name) });

        [Fact]
        public void ReadFromWorldFolder_PicksTheHighestCommittedSave_Numerically()
        {
            WorldSaveFixtures.WriteSave(WorldFolder, 9, SaveNaming("Nine"));
            WorldSaveFixtures.WriteSave(WorldFolder, 10, SaveNaming("Ten"));

            var history = WorldPlayerHistory.ReadFromWorldFolder(WorldFolder);

            Assert.Equal("Ten", Assert.Single(history).DisplayName);
        }

        // Mid-save, the new .fwl2 exists (possibly half-written) before its .ok; the committed save is the right one.
        [Fact]
        public void ReadFromWorldFolder_IgnoresASaveWithoutItsOkMarker()
        {
            WorldSaveFixtures.WriteSave(WorldFolder, 4, SaveNaming("Committed"));
            WorldSaveFixtures.WriteSave(WorldFolder, 5, new byte[] { 1, 2, 3 }, committed: false);

            var history = WorldPlayerHistory.ReadFromWorldFolder(WorldFolder);

            Assert.Equal("Committed", Assert.Single(history).DisplayName);
        }

        [Fact]
        public void ReadFromWorldFolder_NoCommittedSave_ReturnsEmpty()
        {
            WorldSaveFixtures.WriteSave(WorldFolder, 0, SaveNaming("Uncommitted"), committed: false);

            Assert.Empty(WorldPlayerHistory.ReadFromWorldFolder(WorldFolder));
        }

        [Fact]
        public void ReadFromWorldFolder_MissingFolder_ReturnsEmpty()
        {
            Assert.Empty(WorldPlayerHistory.ReadFromWorldFolder(WorldFolder));
        }

        #endregion
    }
}
