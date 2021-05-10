using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

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
            this.Context = context;
            this.FilePath = filePath;

            this.DataFileProvider.DataLoaded += this.OnDataLoaded;
        }

        private Dictionary<string, TEntity> Entities = new();

        public virtual void Load()
        {
            this.DataFileProvider.Load<JsonDataFile<TEntity>>(this.FilePath);
        }

        public virtual void Save()
        {
            this.DataFileProvider.Save(this.FilePath, new JsonDataFile<TEntity>(this.Entities));
        }

        protected virtual void OnDataLoaded(object sender, object dataFile)
        {
            if (dataFile is JsonDataFile<TEntity> typed)
            {
                this.Entities = typed.Data;
                this.DataReady?.Invoke(this, EventArgs.Empty);
            }
        }

        #region IDataRepository<T> implementation

        public event EventHandler DataReady;

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

        public virtual void UpsertBulk(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                this.Entities[entity.Key] = entity;
                this.EntityUpdated?.Invoke(this, entity);
            }

            this.DataUpdated?.Invoke(this, EventArgs.Empty);
            this.Save();
        }

        public virtual void Remove(string key)
        {
            if (this.Entities.TryGetValue(key, out var entity) && this.Entities.Remove(key))
            {
                this.EntityRemoved?.Invoke(this, entity);

                this.DataUpdated?.Invoke(this, EventArgs.Empty);
                this.Save();
            }
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

        public virtual void RemoveBulk(IEnumerable<string> keys)
        {
            var anyRemoved = false;

            foreach (var key in keys)
            {
                if (this.Entities.TryGetValue(key, out var entity) && this.Entities.Remove(key))
                {
                    this.EntityRemoved?.Invoke(this, entity);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
            {
                this.DataUpdated?.Invoke(this, EventArgs.Empty);
                this.Save();
            }
        }

        public virtual void RemoveBulk(IEnumerable<TEntity> entities)
        {
            var anyRemoved = false;

            foreach (var entity in entities)
            {
                if (this.Entities.Remove(entity.Key))
                {
                    this.EntityRemoved?.Invoke(this, entity);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
            {
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
