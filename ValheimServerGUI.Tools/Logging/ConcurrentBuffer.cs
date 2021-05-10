using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Tools.Logging
{
    public class ConcurrentBuffer<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
    {
        public int BufferSize { get; }

        private ConcurrentQueue<T> ConcurrentQueue = new();
        
        public ConcurrentBuffer(int bufferSize)
        {
            if (bufferSize < 0) throw new ArgumentException("Buffer size must be >= 0");

            this.BufferSize = bufferSize;
        }

        public int Count => this.ConcurrentQueue.Count;

        public bool IsReadOnly => false;

        public void Enqueue(T item)
        {
            this.ConcurrentQueue.Enqueue(item);

            while (this.ConcurrentQueue.Count > this.BufferSize)
            {
                this.ConcurrentQueue.TryDequeue(out var _);
            }
        }

        public T Dequeue()
        {
            if (this.ConcurrentQueue.TryDequeue(out var item))
            {
                return item;
            }

            return default;
        }

        public void Clear()
        {
            this.ConcurrentQueue.Clear();
        }

        public bool Contains(T item)
        {
            return this.ConcurrentQueue.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.ConcurrentQueue.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.ConcurrentQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.ConcurrentQueue.GetEnumerator();
        }
    }
}
