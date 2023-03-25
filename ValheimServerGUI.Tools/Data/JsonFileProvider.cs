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

        protected virtual void OnDataLoaded<TFile>(TFile data) where TFile : class
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

        public event EventHandler<object> DataLoaded;

        public event EventHandler<object> DataSaved;

        public virtual Task<TFile> LoadAsync<TFile>(string filePath) where TFile : class
        {
            filePath = Environment.ExpandEnvironmentVariables(filePath);
            TFile dataFile = default;

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
                Logger.Error(e, "Error loading JSON data from file: {filePath}", filePath);
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
                if (!Directory.Exists(filePath))
                {
                    var directory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directory);
                }

                using var streamWriter = File.CreateText(filePath);
                using var jsonWriter = new JsonTextWriter(streamWriter);

                Serializer.Serialize(jsonWriter, data);
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
    }
}
