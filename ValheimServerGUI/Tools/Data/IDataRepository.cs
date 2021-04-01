using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Data
{
    public interface IDataRepository<TEntity> where TEntity : IPrimaryKeyEntity
    {
        event EventHandler DataReady;

        event EventHandler DataUpdated;

        event EventHandler DataCleared;

        event EventHandler<TEntity> EntityUpdated;

        event EventHandler<TEntity> EntityRemoved;

        IEnumerable<TEntity> Data { get; }

        TEntity FindById(string id);

        void Upsert(TEntity entity);

        void UpsertBulk(IEnumerable<TEntity> entities);

        void Remove(TEntity entity);

        void RemoveBulk(IEnumerable<TEntity> entities);

        void RemoveAll();
    }
}
