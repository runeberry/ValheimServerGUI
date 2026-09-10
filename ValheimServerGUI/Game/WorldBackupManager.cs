using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Manual world backup/restore support for both legacy Valheim saves
    /// (.db/.fwl) and the Valheim 1.0 directory/chunk save format.
    /// </summary>
    public static class WorldBackupManager
    {
        private const int BackupFormatVersion = 1;
        private const string ManifestEntryName = "manifest.json";
        private const string WorldEntryPrefix = "world/";

        private enum WorldSaveFormat
        {
            LegacyFlatFiles,
            Valheim1Directory,
        }

        private sealed class WorldLocation
        {
            public string WorldName { get; init; }
            public DirectoryInfo WorldsFolder { get; init; }
            public WorldSaveFormat Format { get; init; }
            public DirectoryInfo WorldDirectory { get; init; }
            public List<FileInfo> LegacyFiles { get; init; } = new();
        }

        private sealed class BackupManifest
        {
            public int FormatVersion { get; set; }
            public string WorldName { get; set; }
            public string SaveFormat { get; set; }
            public string WorldsFolderName { get; set; }
            public DateTime CreatedUtc { get; set; }
            public string BackupKind { get; set; }
        }

        /// <summary>
        /// Manual backups are stored outside Valheim's save tree so that Valheim
        /// will never interpret them as playable worlds or native rolling backups.
        /// </summary>
        public static DirectoryInfo GetBackupRoot()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(localAppData, "ValheimServerGUI", "Backups");
            return Directory.CreateDirectory(path);
        }

        public static DirectoryInfo GetWorldBackupDirectory(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName))
                throw new ArgumentException("World name cannot be empty.", nameof(worldName));

            var safeWorldName = MakeSafeFileName(worldName);
            return Directory.CreateDirectory(Path.Combine(GetBackupRoot().FullName, safeWorldName));
        }

        /// <summary>
        /// Creates a ZIP containing one complete, internally consistent copy of
        /// the selected world. The server should be stopped before this is called.
        /// </summary>
        public static FileInfo CreateBackup(DirectoryInfo saveDataFolder, string worldName, string backupKind = "manual")
        {
            if (saveDataFolder == null) throw new ArgumentNullException(nameof(saveDataFolder));
            if (string.IsNullOrWhiteSpace(worldName)) throw new ArgumentException("World name cannot be empty.", nameof(worldName));

            var location = FindWorld(saveDataFolder, worldName)
                ?? throw new FileNotFoundException($"Unable to find world '{worldName}'.");

            var backupDir = GetWorldBackupDirectory(worldName);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var safeKind = MakeSafeFileName(string.IsNullOrWhiteSpace(backupKind) ? "manual" : backupKind);
            var backupPath = Path.Combine(backupDir.FullName, $"{MakeSafeFileName(worldName)}_{safeKind}_{timestamp}.zip");

            // Avoid accidental overwrite if two backups are requested during the same second.
            var suffix = 1;
            while (File.Exists(backupPath))
            {
                backupPath = Path.Combine(backupDir.FullName,
                    $"{MakeSafeFileName(worldName)}_{safeKind}_{timestamp}_{suffix++}.zip");
            }

            var manifest = new BackupManifest
            {
                FormatVersion = BackupFormatVersion,
                WorldName = worldName,
                SaveFormat = location.Format.ToString(),
                WorldsFolderName = location.WorldsFolder.Name,
                CreatedUtc = DateTime.UtcNow,
                BackupKind = backupKind,
            };

            using var fileStream = new FileStream(backupPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false);

            var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(manifestEntry.Open()))
            {
                writer.Write(JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            }

            if (location.Format == WorldSaveFormat.Valheim1Directory)
            {
                AddDirectoryToArchive(archive, location.WorldDirectory, WorldEntryPrefix);
            }
            else
            {
                foreach (var file in location.LegacyFiles)
                {
                    AddFileToArchive(archive, file, WorldEntryPrefix + file.Name);
                }
            }

            return new FileInfo(backupPath);
        }

        /// <summary>
        /// Restores a backup into the current save tree. Before replacing anything,
        /// a pre-restore safety backup of the current world is created automatically.
        /// The server should be stopped before this is called.
        /// </summary>
        public static FileInfo RestoreBackup(DirectoryInfo saveDataFolder, string selectedWorldName, FileInfo backupFile)
        {
            if (saveDataFolder == null) throw new ArgumentNullException(nameof(saveDataFolder));
            if (backupFile == null) throw new ArgumentNullException(nameof(backupFile));
            if (!backupFile.Exists) throw new FileNotFoundException("Backup file does not exist.", backupFile.FullName);
            if (string.IsNullOrWhiteSpace(selectedWorldName)) throw new ArgumentException("World name cannot be empty.", nameof(selectedWorldName));

            BackupManifest manifest;
            using (var archive = ZipFile.OpenRead(backupFile.FullName))
            {
                manifest = ReadAndValidateManifest(archive);
            }

            if (!string.Equals(manifest.WorldName, selectedWorldName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"This backup belongs to world '{manifest.WorldName}', but '{selectedWorldName}' is selected.");
            }

            var existingWorld = FindWorld(saveDataFolder, selectedWorldName)
                ?? throw new FileNotFoundException($"Unable to find current world '{selectedWorldName}'.");

            // This is intentionally done before touching the current save.
            var safetyBackup = CreateBackup(saveDataFolder, selectedWorldName, "pre-restore");

            var tempRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),
                "ValheimServerGUI-Restore-" + Guid.NewGuid().ToString("N")));

            try
            {
                var tempWorld = Directory.CreateDirectory(Path.Combine(tempRoot.FullName, "world"));
                ExtractWorldPayload(backupFile, tempWorld);

                var format = ParseSaveFormat(manifest.SaveFormat);
                ValidateExtractedPayload(tempWorld, manifest.WorldName, format);

                var worldsFolderName = manifest.WorldsFolderName;
                if (!string.Equals(worldsFolderName, "worlds", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(worldsFolderName, "worlds_local", StringComparison.OrdinalIgnoreCase))
                {
                    // Old/custom archive: restore where the current copy already lives.
                    worldsFolderName = existingWorld.WorldsFolder.Name;
                }

                var targetWorldsFolder = Directory.CreateDirectory(Path.Combine(saveDataFolder.FullName, worldsFolderName));

                if (format == WorldSaveFormat.Valheim1Directory)
                {
                    // Remove any current representation of this world first so that
                    // stale chunk files can never survive a restore.
                    RemoveWorldRepresentations(saveDataFolder, selectedWorldName);

                    var targetDir = Path.Combine(targetWorldsFolder.FullName, selectedWorldName);
                    CopyDirectory(tempWorld, new DirectoryInfo(targetDir));
                }
                else
                {
                    RemoveWorldRepresentations(saveDataFolder, selectedWorldName);
                    foreach (var file in tempWorld.GetFiles("*", SearchOption.TopDirectoryOnly))
                    {
                        file.CopyTo(Path.Combine(targetWorldsFolder.FullName, file.Name), true);
                    }
                }

                return safetyBackup;
            }
            catch
            {
                // If replacement failed after the safety copy was made, leave that
                // copy untouched so the user always has a recovery point.
                throw;
            }
            finally
            {
                try
                {
                    if (tempRoot.Exists) tempRoot.Delete(true);
                }
                catch
                {
                    // Best-effort temp cleanup only.
                }
            }
        }

        private static WorldLocation FindWorld(DirectoryInfo saveDataFolder, string worldName)
        {
            // Prefer worlds_local, which is the normal location for dedicated/local saves.
            foreach (var folderName in new[] { "worlds_local", "worlds" })
            {
                var worldsFolder = new DirectoryInfo(Path.Combine(saveDataFolder.FullName, folderName));
                if (!worldsFolder.Exists) continue;

                var worldDirectory = new DirectoryInfo(Path.Combine(worldsFolder.FullName, worldName));
                if (worldDirectory.Exists && worldDirectory.GetFiles("_main.*.fwl2", SearchOption.TopDirectoryOnly).Any())
                {
                    return new WorldLocation
                    {
                        WorldName = worldName,
                        WorldsFolder = worldsFolder,
                        Format = WorldSaveFormat.Valheim1Directory,
                        WorldDirectory = worldDirectory,
                    };
                }

                var fwl = new FileInfo(Path.Combine(worldsFolder.FullName, worldName + ".fwl"));
                if (fwl.Exists)
                {
                    // Include the current legacy world files and any .old companions,
                    // but never Valheim's _backup_ files.
                    var legacyFiles = worldsFolder
                        .GetFiles(worldName + ".*", SearchOption.TopDirectoryOnly)
                        .Where(f => !f.Name.Contains("_backup_", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return new WorldLocation
                    {
                        WorldName = worldName,
                        WorldsFolder = worldsFolder,
                        Format = WorldSaveFormat.LegacyFlatFiles,
                        LegacyFiles = legacyFiles,
                    };
                }
            }

            return null;
        }

        private static void AddDirectoryToArchive(ZipArchive archive, DirectoryInfo sourceDirectory, string archivePrefix)
        {
            foreach (var file in sourceDirectory.GetFiles("*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDirectory.FullName, file.FullName).Replace('\\', '/');
                AddFileToArchive(archive, file, archivePrefix + relative);
            }
        }

        private static void AddFileToArchive(ZipArchive archive, FileInfo sourceFile, string entryName)
        {
            var entry = archive.CreateEntry(entryName.Replace('\\', '/'), CompressionLevel.Optimal);
            using var input = sourceFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            using var output = entry.Open();
            input.CopyTo(output);
        }

        private static BackupManifest ReadAndValidateManifest(ZipArchive archive)
        {
            var entry = archive.GetEntry(ManifestEntryName)
                ?? throw new InvalidDataException("This ZIP is not a ValheimServerGUI world backup (manifest.json is missing).");

            BackupManifest manifest;
            using (var reader = new StreamReader(entry.Open()))
            {
                manifest = JsonSerializer.Deserialize<BackupManifest>(reader.ReadToEnd());
            }

            if (manifest == null)
                throw new InvalidDataException("The backup manifest is invalid.");
            if (manifest.FormatVersion != BackupFormatVersion)
                throw new InvalidDataException($"Unsupported backup format version: {manifest.FormatVersion}.");
            if (string.IsNullOrWhiteSpace(manifest.WorldName))
                throw new InvalidDataException("The backup manifest does not contain a world name.");

            ParseSaveFormat(manifest.SaveFormat); // validate now
            return manifest;
        }

        private static WorldSaveFormat ParseSaveFormat(string value)
        {
            if (!Enum.TryParse<WorldSaveFormat>(value, true, out var format))
                throw new InvalidDataException($"Unsupported world save format: {value}.");
            return format;
        }

        private static void ExtractWorldPayload(FileInfo backupFile, DirectoryInfo destination)
        {
            using var archive = ZipFile.OpenRead(backupFile.FullName);
            var payloadEntries = archive.Entries
                .Where(e => e.FullName.StartsWith(WorldEntryPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .ToList();

            if (payloadEntries.Count == 0)
                throw new InvalidDataException("The backup does not contain any world files.");

            var destinationRoot = Path.GetFullPath(destination.FullName + Path.DirectorySeparatorChar);

            foreach (var entry in payloadEntries)
            {
                var relativePath = entry.FullName.Substring(WorldEntryPrefix.Length).Replace('/', Path.DirectorySeparatorChar);
                var destinationPath = Path.GetFullPath(Path.Combine(destination.FullName, relativePath));

                // Protect against path traversal in malformed/tampered archives.
                if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Backup contains an invalid file path.");

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                entry.ExtractToFile(destinationPath, true);
            }
        }

        private static void ValidateExtractedPayload(DirectoryInfo tempWorld, string worldName, WorldSaveFormat format)
        {
            if (format == WorldSaveFormat.Valheim1Directory)
            {
                if (!tempWorld.GetFiles("_main.*.fwl2", SearchOption.TopDirectoryOnly).Any())
                    throw new InvalidDataException("The Valheim 1.0 backup is missing its _main.*.fwl2 metadata file.");
                if (!tempWorld.GetFiles("_main.*.db2", SearchOption.TopDirectoryOnly).Any())
                    throw new InvalidDataException("The Valheim 1.0 backup is missing its _main.*.db2 database file.");
            }
            else
            {
                if (!File.Exists(Path.Combine(tempWorld.FullName, worldName + ".fwl")))
                    throw new InvalidDataException("The legacy backup is missing its .fwl file.");
                if (!File.Exists(Path.Combine(tempWorld.FullName, worldName + ".db")))
                    throw new InvalidDataException("The legacy backup is missing its .db file.");
            }
        }

        private static void RemoveWorldRepresentations(DirectoryInfo saveDataFolder, string worldName)
        {
            foreach (var folderName in new[] { "worlds_local", "worlds" })
            {
                var worldsFolder = new DirectoryInfo(Path.Combine(saveDataFolder.FullName, folderName));
                if (!worldsFolder.Exists) continue;

                var worldDirectory = new DirectoryInfo(Path.Combine(worldsFolder.FullName, worldName));
                if (worldDirectory.Exists) worldDirectory.Delete(true);

                foreach (var file in worldsFolder
                    .GetFiles(worldName + ".*", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.Name.Contains("_backup_", StringComparison.OrdinalIgnoreCase)))
                {
                    file.Delete();
                }
            }
        }

        private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            destination.Create();

            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(destination.FullName, file.Name), true);

            foreach (var directory in source.GetDirectories())
                CopyDirectory(directory, new DirectoryInfo(Path.Combine(destination.FullName, directory.Name)));
        }

        private static string MakeSafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            var safe = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "world" : safe;
        }
    }
}
