using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Data
{
    public interface ILocalDataProvider
    {
        public void Load();

        public void Save();

        public event EventHandler DataLoaded;

        public event EventHandler DataSaved;
    }

    public interface ILocalDataProvider<TEntity> : ILocalDataProvider
        where TEntity : IPrimaryKeyEntity
    {
        public IEnumerable<TEntity> Data { get; }

        public TEntity FindById(string id);

        public void Upsert(TEntity entity);

        public void UpsertBulk(IEnumerable<TEntity> entities);

        public void Remove(TEntity entity);

        public void RemoveBulk(IEnumerable<TEntity> entities);

        public void RemoveAll();

        public event EventHandler DataUpdated;

        public event EventHandler DataCleared;

        public event EventHandler<TEntity> EntityUpdated;

        public event EventHandler<TEntity> EntityRemoved;
    }
}
