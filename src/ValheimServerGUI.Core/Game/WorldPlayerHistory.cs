using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// One entry of a world's player history: a player who has joined the world, with the display name their
    /// client reported for itself (Steam persona, Xbox gamertag, ...). Mirrors the game's
    /// <c>ZNet.CrossNetworkUserInfo</c>.
    /// </summary>
    /// <param name="PlatformUserId">The game's <c>PlatformUserID</c> string, e.g. <c>Steam_76561198000000001</c>.</param>
    /// <param name="DisplayName">The name the player's client reported. Self-reported, so display-only.</param>
    /// <param name="ServerAssignedDisplayName">The display name, de-duplicated by the server (<c>Name#2</c>).</param>
    /// <param name="PlayFabId">The player's PlayFab id (crossplay), or empty.</param>
    public record WorldPlayerHistoryEntry(
        string PlatformUserId,
        string DisplayName,
        string ServerAssignedDisplayName,
        string PlayFabId);

    /// <summary>
    /// Reads the player history from a world's metadata save (<c>worlds_local/&lt;World&gt;/_main.&lt;N&gt;.fwl2</c>).
    /// The server adds every player who joins and never removes anyone, and rewrites the file on every world save.
    ///
    /// <para>Layout, from <c>World.SaveWorldFWLData</c> / <c>World.LoadWorldMeta</c> (BinaryWriter encoding:
    /// little-endian ints, 7-bit length-prefixed UTF-8 strings, 1-byte bools):</para>
    /// <code>
    /// int32  payloadLength            // bytes that follow
    /// int32  version                  // 41 as of Valheim 1.0.17
    /// string name, string seedName, int32 seed, int64 uid
    /// int32  worldGenVersion          // version &gt;= 26
    /// bool   needsDB                  // version &gt;= 30
    /// int32  count, string × count    // starting global keys, version &gt;= 32
    /// int32  count, entry × count     // player history, version &gt;= 41
    ///   string id, string displayName, string serverAssignedDisplayName, string playfabId
    /// </code>
    /// <para>The history is the last section, so a successful parse ends exactly at the payload end. That check is
    /// what lets a newer, unknown version be read best-effort: if the layout changed, the bytes will not line up
    /// and the read fails instead of returning garbage.</para>
    /// </summary>
    public static class WorldPlayerHistory
    {
        /// <summary>The first world version that saves the player history (<c>Version.World.DeepNorth</c>).</summary>
        public const int PlayerHistoryVersion = 41;

        // The smallest possible history entry: four empty strings, one length byte each.
        private const int MinEntryBytes = 4;

        // The smallest possible starting global key: one empty string.
        private const int MinGlobalKeyBytes = 1;

        private static readonly Regex SaveFileRegex = new(@"^_main\.(\d+)\.fwl2$", RegexOptions.Compiled);

        /// <summary>
        /// Reads the player history from the newest <b>complete</b> save in a world folder. Returns an empty list when
        /// the folder has no complete save. Throws <see cref="IOException"/> or <see cref="InvalidDataException"/>
        /// when the save cannot be read.
        /// </summary>
        public static IReadOnlyList<WorldPlayerHistoryEntry> ReadFromWorldFolder(DirectoryInfo worldFolder)
        {
            var save = FindLatestCompleteSave(worldFolder);
            if (save == null) return Array.Empty<WorldPlayerHistoryEntry>();

            // Share delete as well as write: the server deletes the previous save's files right after it commits a
            // new one, and on Windows an open handle without FileShare.Delete would make that delete fail.
            using var stream = new FileStream(
                save.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Read(stream);
        }

        /// <summary>
        /// Finds the newest save whose write completed. The server writes files directly (no temp-and-rename) and
        /// commits a save by writing <c>_main.&lt;N&gt;.ok</c> after the <c>.fwl2</c>, then deletes the previous
        /// save. A <c>.fwl2</c> without its <c>.ok</c> is still being written, or its save failed.
        /// </summary>
        public static FileInfo? FindLatestCompleteSave(DirectoryInfo worldFolder)
        {
            if (!worldFolder.Exists) return null;

            return worldFolder.GetFiles("_main.*.fwl2")
                .Select(file => (file, match: SaveFileRegex.Match(file.Name)))
                .Where(f => f.match.Success && ulong.TryParse(f.match.Groups[1].Value, out _))
                .Select(f => (f.file, number: ulong.Parse(f.match.Groups[1].Value)))
                .Where(f => File.Exists(Path.Join(worldFolder.FullName, $"_main.{f.number}.ok")))
                .OrderByDescending(f => f.number)
                .Select(f => f.file)
                .FirstOrDefault();
        }

        /// <summary>
        /// Reads the player history from a <c>.fwl2</c> stream, front to back without buffering the file. Worlds
        /// saved before <see cref="PlayerHistoryVersion"/> have no history and return an empty list. Throws
        /// <see cref="InvalidDataException"/> when the data is truncated or does not match the layout.
        /// </summary>
        public static IReadOnlyList<WorldPlayerHistoryEntry> Read(Stream stream)
        {
            if (!stream.CanSeek) throw new ArgumentException("The stream must be seekable.", nameof(stream));

            try
            {
                using var binary = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                var payloadLength = binary.ReadInt32();
                var payloadEnd = stream.Position + payloadLength;
                if (payloadLength < sizeof(int) || payloadEnd > stream.Length)
                {
                    throw new InvalidDataException(
                        $"World save declares a {payloadLength}-byte payload but the file has {stream.Length - stream.Position} bytes left.");
                }

                var reader = new PayloadReader(binary, payloadEnd);

                var version = reader.ReadInt32();
                if (version < PlayerHistoryVersion) return Array.Empty<WorldPlayerHistoryEntry>();

                // Every field below is present from PlayerHistoryVersion on; only the history is kept.
                reader.ReadString(); // name
                reader.ReadString(); // seedName
                reader.ReadInt32(); // seed
                reader.ReadInt64(); // uid
                reader.ReadInt32(); // worldGenVersion
                reader.ReadBoolean(); // needsDB

                var globalKeyCount = reader.ReadCount(MinGlobalKeyBytes);
                for (var i = 0; i < globalKeyCount; i++) reader.ReadString();

                var historyCount = reader.ReadCount(MinEntryBytes);
                var history = new List<WorldPlayerHistoryEntry>(historyCount);
                for (var i = 0; i < historyCount; i++)
                {
                    history.Add(new WorldPlayerHistoryEntry(
                        PlatformUserId: reader.ReadString(),
                        DisplayName: reader.ReadString(),
                        ServerAssignedDisplayName: reader.ReadString(),
                        PlayFabId: reader.ReadString()));
                }

                if (reader.Remaining != 0)
                {
                    throw new InvalidDataException(
                        $"World save (version {version}) has {reader.Remaining} unread bytes after the player history; the layout is not recognized.");
                }

                return history;
            }
            catch (Exception e) when (e is EndOfStreamException or FormatException)
            {
                throw new InvalidDataException("World save is truncated or malformed.", e);
            }
        }

        /// <summary>
        /// Reads primitives from the payload, checking fixed-size reads and counts against the bytes left in it, so a
        /// corrupt count can never size a list larger than the payload could hold.
        /// </summary>
        private sealed class PayloadReader(BinaryReader binary, long payloadEnd)
        {
            public long Remaining => payloadEnd - binary.BaseStream.Position;

            public int ReadInt32()
            {
                Require(sizeof(int));
                return binary.ReadInt32();
            }

            public long ReadInt64()
            {
                Require(sizeof(long));
                return binary.ReadInt64();
            }

            public bool ReadBoolean()
            {
                Require(sizeof(bool));
                return binary.ReadBoolean();
            }

            /// <summary>Reads an item count, rejecting one that could not fit in the bytes left.</summary>
            public int ReadCount(int minItemBytes)
            {
                var count = ReadInt32();
                if (count < 0 || count > Remaining / minItemBytes)
                {
                    throw new InvalidDataException($"World save declares {count} items but only {Remaining} bytes remain.");
                }
                return count;
            }

            // BinaryReader reads a string in small chunks, so a corrupt length fails at end of stream instead of
            // allocating it. A string that overruns the payload leaves Remaining negative, and the next read fails.
            public string ReadString() => binary.ReadString();

            private void Require(long bytes)
            {
                if (bytes < 0 || bytes > Remaining)
                {
                    throw new InvalidDataException($"World save needs {bytes} more bytes but only {Remaining} remain.");
                }
            }
        }
    }
}
