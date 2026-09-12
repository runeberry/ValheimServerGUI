using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public class JsonFileProvider : IFileProvider
    {
        private readonly JsonSerializer Serializer = new() { Formatting = Formatting.Indented };
        private readonly ReaderWriterLockSlim RWLock = new();

        protected readonly ILogger Logger;

        public JsonFileProvider(ILogger logger)
        {
            Logger = logger;
        }

        protected virtual void OnDataLoaded<TFile>(TFile? data) where TFile : class
        {
            if (data == null) return;
            DataLoaded?.Invoke(this, data);
        }

        protected virtual void OnDataSaved<TFile>(TFile data) where TFile : class
        {
            if (data == null) return;
            DataSaved?.Invoke(this, data);
        }

        #region ILocalDataProvider implementation

        public event EventHandler<object>? DataLoaded;

        public event EventHandler<object>? DataSaved;

        public virtual Task<TFile?> LoadAsync<TFile>(string filePath) where TFile : class
        {
            filePath = Environment.ExpandEnvironmentVariables(filePath);
            TFile? dataFile = default;

            RWLock.EnterReadLock();

            try
            {
                if (File.Exists(filePath))
                {
                    using var streamReader = File.OpenText(filePath);
                    using var jsonReader = new JsonTextReader(streamReader);

                    dataFile = Serializer.Deserialize<TFile>(jsonReader);
                }
            }
            catch (Exception e)
            {
                // §15 #8: a corrupt file must not be silently overwritten with defaults. Back it up
                // aside so the data is recoverable, then fall through to defaults (the caller treats a
                // null/absent file as "use defaults"). The subsequent save then writes a fresh file
                // without destroying the corrupt original.
                Logger.Error(e, "Error loading JSON data from file: {filePath}", filePath);
                BackupCorruptFile(filePath);
            }
            finally
            {
                RWLock.ExitReadLock();
            }

            OnDataLoaded(dataFile);

            return Task.FromResult(dataFile);
        }

        public virtual Task SaveAsync<TFile>(string filePath, TFile data) where TFile : class
        {
            filePath = Environment.ExpandEnvironmentVariables(filePath);

            RWLock.EnterWriteLock();

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // §15 #8: write to a temp file then atomically replace the target, so a crash mid-write
                // never leaves a half-written (corrupt) config behind.
                var tempPath = filePath + ".tmp";
                try
                {
                    using (var streamWriter = File.CreateText(tempPath))
                    using (var jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        Serializer.Serialize(jsonWriter, data);
                    }

                    File.Move(tempPath, filePath, overwrite: true);
                }
                catch
                {
                    // Never leave a stray temp file behind on failure.
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
                    throw;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error saving JSON data to file: {filePath}", filePath);
            }
            finally
            {
                RWLock.ExitWriteLock();
            }

            OnDataSaved(data);

            return Task.CompletedTask;
        }

        #endregion

        private void BackupCorruptFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var backupPath = $"{filePath}.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                File.Move(filePath, backupPath, overwrite: true);
                Logger.Warning("Backed up corrupt file {filePath} to {backupPath}", filePath, backupPath);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to back up corrupt file: {filePath}", filePath);
            }
        }
    }
}
