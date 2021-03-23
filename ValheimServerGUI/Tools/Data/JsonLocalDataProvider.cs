using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public class JsonLocalDataProvider<TEntity> : ILocalDataProvider<TEntity>
        where TEntity : IPrimaryKeyEntity
    {
        private readonly string FilePath;
        private readonly JsonSerializer Serializer = new();
        private readonly ReaderWriterLockSlim RWLock = new();

        private Dictionary<string, TEntity> Entities = new();

        public JsonLocalDataProvider(string filePath)
        {
            this.FilePath = filePath;
        }

        #region ILocalDataProvider implementation

        public event EventHandler DataLoaded;

        public event EventHandler DataSaved;

        public virtual void Load()
        {
            Task.Run(() =>
            {
                if (!File.Exists(this.FilePath))
                {
                    File.Create(this.FilePath);
                }

                using var streamReader = File.OpenText(this.FilePath);
                using var jsonReader = new JsonTextReader(streamReader);

                var dataFile = this.Serializer.Deserialize<JsonDataFile<TEntity>>(jsonReader);

                this.Entities = dataFile.Data;
                this.DataLoaded?.Invoke(this, EventArgs.Empty);
            });
        }

        public virtual void Save()
        {
            Task.Run(() =>
            {
                this.RWLock.EnterWriteLock();

                try
                {
                    using var streamWriter = File.CreateText(this.FilePath);
                    using var jsonWriter = new JsonTextWriter(streamWriter);

                    var dataFile = new JsonDataFile<TEntity> { Data = this.Entities };
                    this.Serializer.Serialize(jsonWriter, dataFile);

                    this.DataSaved?.Invoke(this, EventArgs.Empty);
                }
                finally
                {
                    this.RWLock.ExitWriteLock();
                }
            });
        }

        #endregion

        #region ILocalDataProvider<T> implementation

        public event EventHandler DataUpdated;

        public event EventHandler DataCleared;

        public event EventHandler<TEntity> EntityUpdated;

        public event EventHandler<TEntity> EntityRemoved;

        public IEnumerable<TEntity> Data => this.Entities.Values;

        public virtual TEntity FindById(string id)
        {
            if (this.Entities.TryGetValue(id, out var entity))
            {
                return entity;
            }

            return default;
        }

        public virtual void Upsert(TEntity entity)
        {
            this.Entities[entity.Key] = entity;

            this.EntityUpdated?.Invoke(this, entity);
            this.DataUpdated?.Invoke(this, EventArgs.Empty);

            this.Save();
        }

        public virtual void Remove(TEntity entity)
        {
            if (this.Entities.Remove(entity.Key))
            {
                this.EntityRemoved?.Invoke(this, entity);
                this.DataUpdated?.Invoke(this, EventArgs.Empty);
                
                this.Save();
            }
        }

        public virtual void RemoveAll()
        {
            if (this.Entities.Any())
            {
                this.Entities.Clear();

                this.DataCleared?.Invoke(this, EventArgs.Empty);

                this.Save();
            }
        }

        #endregion
    }
}
