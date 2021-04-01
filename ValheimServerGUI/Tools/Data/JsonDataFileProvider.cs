using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public class JsonDataFileProvider
    {
        private readonly string FilePath;
        private readonly JsonSerializer Serializer = new();
        private readonly ReaderWriterLockSlim RWLock = new();

        private readonly ILogger Logger;

        public JsonDataFileProvider(string filePath, ILogger logger)
        {
            this.FilePath = Environment.ExpandEnvironmentVariables(filePath);
            this.Logger = logger;
        }

        protected virtual void OnDataLoaded<TFile>(TFile data) where TFile : class
        {
            if (data == null) return;
            this.DataLoaded?.Invoke(this, data);
        }

        protected virtual void OnDataSaved<TFile>(TFile data) where TFile : class
        {
            if (data == null) return;
            this.DataSaved?.Invoke(this, data);
        }

        #region ILocalDataProvider implementation

        public event EventHandler<object> DataLoaded;

        public event EventHandler<object> DataSaved;

        public virtual Task<TFile> LoadAsync<TFile>() where TFile : class
        {
            TFile dataFile = default;

            this.RWLock.EnterReadLock();

            try
            {
                if (File.Exists(FilePath))
                {
                    using var streamReader = File.OpenText(this.FilePath);
                    using var jsonReader = new JsonTextReader(streamReader);

                    dataFile = this.Serializer.Deserialize<TFile>(jsonReader);
                }
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Error loading JSON data from file: {0}", this.FilePath);
            }
            finally
            {
                this.RWLock.ExitReadLock();
            }

            this.OnDataLoaded(dataFile);

            return Task.FromResult(dataFile);
        }

        public virtual Task SaveAsync<TFile>(TFile data) where TFile : class
        {
            this.RWLock.EnterWriteLock();

            try
            {
                using var streamWriter = File.CreateText(this.FilePath);
                using var jsonWriter = new JsonTextWriter(streamWriter);

                this.Serializer.Serialize(jsonWriter, data);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Error saving JSON data to file: {0}", this.FilePath);
            }
            finally
            {
                this.RWLock.ExitWriteLock();
            }

            this.OnDataSaved(data);

            return Task.CompletedTask;
        }

        #endregion
    }
}
