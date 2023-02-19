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

            BufferSize = bufferSize;
        }

        public int Count => ConcurrentQueue.Count;

        public bool IsReadOnly => false;

        public void Enqueue(T item)
        {
            ConcurrentQueue.Enqueue(item);

            while (ConcurrentQueue.Count > BufferSize)
            {
                ConcurrentQueue.TryDequeue(out var _);
            }
        }

        public T Dequeue()
        {
            if (ConcurrentQueue.TryDequeue(out var item))
            {
                return item;
            }

            return default;
        }

        public void Clear()
        {
            ConcurrentQueue.Clear();
        }

        public bool Contains(T item)
        {
            return ConcurrentQueue.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ConcurrentQueue.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ConcurrentQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ConcurrentQueue.GetEnumerator();
        }
    }
}
