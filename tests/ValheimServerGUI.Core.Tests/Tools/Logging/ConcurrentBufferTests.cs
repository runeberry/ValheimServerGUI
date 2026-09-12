using System;
using System.Linq;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools.Logging
{
    /// <summary>
    /// E47: the ring buffer backing the log view evicts oldest-first, caps its count at the buffer
    /// size, and preserves FIFO order of the survivors.
    /// </summary>
    public class ConcurrentBufferTests
    {
        [Fact]
        public void Enqueue_WithinCapacity_KeepsEverythingInOrder()
        {
            var buffer = new ConcurrentBuffer<int>(3);

            buffer.Enqueue(1);
            buffer.Enqueue(2);

            Assert.Equal(2, buffer.Count);
            Assert.Equal(new[] { 1, 2 }, buffer.ToArray());
        }

        [Fact]
        public void Enqueue_BeyondCapacity_EvictsOldest_AndCapsCount()
        {
            var buffer = new ConcurrentBuffer<int>(3);

            foreach (var i in Enumerable.Range(1, 5)) buffer.Enqueue(i);

            // Oldest (1, 2) evicted; count capped at the buffer size; survivors keep FIFO order.
            Assert.Equal(3, buffer.Count);
            Assert.Equal(new[] { 3, 4, 5 }, buffer.ToArray());
        }

        [Fact]
        public void Enqueue_ZeroCapacity_RetainsNothing()
        {
            var buffer = new ConcurrentBuffer<int>(0);

            buffer.Enqueue(1);
            buffer.Enqueue(2);

            Assert.Empty(buffer);
        }

        [Fact]
        public void Constructor_NegativeSize_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ConcurrentBuffer<int>(-1));
        }
    }
}
