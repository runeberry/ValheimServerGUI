using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        public virtual Task<TFile> LoadAsync<TFile>(string filePath) where TFile : class
        {
            filePath = Environment.ExpandEnvironmentVariables(filePath);
            TFile dataFile = default;

            this.RWLock.EnterReadLock();

            try
            {
                if (File.Exists(filePath))
                {
                    using var streamReader = File.OpenText(filePath);
                    using var jsonReader = new JsonTextReader(streamReader);

                    dataFile = this.Serializer.Deserialize<TFile>(jsonReader);
                }
            }
            catch (Exception e)
            {
                this.LogException(e, $"Error loading JSON data from file: {filePath}");
            }
            finally
            {
                this.RWLock.ExitReadLock();
            }

            this.OnDataLoaded(dataFile);

            return Task.FromResult(dataFile);
        }

        public virtual Task SaveAsync<TFile>(string filePath, TFile data) where TFile : class
        {
            filePath = Environment.ExpandEnvironmentVariables(filePath);

            this.RWLock.EnterWriteLock();

            try
            {
                using var streamWriter = File.CreateText(filePath);
                using var jsonWriter = new JsonTextWriter(streamWriter);

                this.Serializer.Serialize(jsonWriter, data);
            }
            catch (Exception e)
            {
                this.LogException(e, $"Error saving JSON data to file: {filePath}");
            }
            finally
            {
                this.RWLock.ExitWriteLock();
            }

            this.OnDataSaved(data);

            return Task.CompletedTask;
        }

        #endregion

        #region Non-public methods

        protected void LogException(Exception e, string message)
        {
            this.Logger.LogError($"{message} - {e.GetType().Name}: {e.Message}");
            this.Logger.LogError(e.StackTrace);
        }

        #endregion
    }
}
