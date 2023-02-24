using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public class DataFileRepository<TEntity> : IDataRepository<TEntity>
        where TEntity : IPrimaryKeyEntity
    {
        protected IDataFileRepositoryContext Context { get; }
        protected string FilePath { get; }

        protected IFileProvider DataFileProvider => Context.DataFileProvider;
        protected ILogger Logger => Context.Logger;

        public DataFileRepository(IDataFileRepositoryContext context, string filePath)
        {
            Context = context;
            FilePath = filePath;

            DataFileProvider.DataLoaded += OnDataLoaded;
        }

        private Dictionary<string, TEntity> Entities = new();

        public virtual void Load()
        {
            DataFileProvider.Load<JsonDataFile<TEntity>>(FilePath);
        }

        public virtual Task LoadAsync()
        {
            return DataFileProvider.LoadAsync<JsonDataFile<TEntity>>(FilePath);
        }

        public virtual void Save()
        {
            DataFileProvider.Save(FilePath, new JsonDataFile<TEntity>(Entities));
        }

        public virtual Task SaveAsync()
        {
            return DataFileProvider.SaveAsync(FilePath, new JsonDataFile<TEntity>(Entities));
        }

        protected virtual void OnDataLoaded(object sender, object dataFile)
        {
            if (dataFile is JsonDataFile<TEntity> typed)
            {
                Entities = typed.Data;
                DataReady?.Invoke(this, EventArgs.Empty);
            }
        }

        #region IDataRepository<T> implementation

        public event EventHandler DataReady;

        public event EventHandler DataUpdated;

        public event EventHandler DataCleared;

        public event EventHandler<TEntity> EntityUpdated;

        public event EventHandler<TEntity> EntityRemoved;

        public IEnumerable<TEntity> Data => Entities.Values;

        public virtual TEntity FindById(string id)
        {
            if (Entities.TryGetValue(id, out var entity))
            {
                return entity;
            }

            return default;
        }

        public virtual void Upsert(TEntity entity)
        {
            Entities[entity.Key] = entity;
            EntityUpdated?.Invoke(this, entity);

            DataUpdated?.Invoke(this, EventArgs.Empty);
            Save();
        }

        public virtual void UpsertBulk(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Entities[entity.Key] = entity;
                EntityUpdated?.Invoke(this, entity);
            }

            DataUpdated?.Invoke(this, EventArgs.Empty);
            Save();
        }

        public virtual void Remove(string key)
        {
            if (Entities.TryGetValue(key, out var entity) && Entities.Remove(key))
            {
                EntityRemoved?.Invoke(this, entity);

                DataUpdated?.Invoke(this, EventArgs.Empty);
                Save();
            }
        }

        public virtual void Remove(TEntity entity)
        {
            if (Entities.Remove(entity.Key))
            {
                EntityRemoved?.Invoke(this, entity);

                DataUpdated?.Invoke(this, EventArgs.Empty);
                Save();
            }
        }

        public virtual void RemoveBulk(IEnumerable<string> keys)
        {
            var anyRemoved = false;

            foreach (var key in keys)
            {
                if (Entities.TryGetValue(key, out var entity) && Entities.Remove(key))
                {
                    EntityRemoved?.Invoke(this, entity);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
            {
                DataUpdated?.Invoke(this, EventArgs.Empty);
                Save();
            }
        }

        public virtual void RemoveBulk(IEnumerable<TEntity> entities)
        {
            var anyRemoved = false;

            foreach (var entity in entities)
            {
                if (Entities.Remove(entity.Key))
                {
                    EntityRemoved?.Invoke(this, entity);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
            {
                DataUpdated?.Invoke(this, EventArgs.Empty);
                Save();
            }
        }

        public virtual void RemoveAll()
        {
            if (Entities.Any())
            {
                Entities.Clear();

                DataCleared?.Invoke(this, EventArgs.Empty);

                Save();
            }
        }

        #endregion
    }
}
