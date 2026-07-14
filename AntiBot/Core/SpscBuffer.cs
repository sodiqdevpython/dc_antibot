using System.Runtime.CompilerServices;
using System.Threading;

namespace dc_antibot.AntiBot.Core
{
    /// <summary>
    /// Single-Producer Single-Consumer lock-free ring buffer.
    /// Producer path (TryEnqueue) — 1 local read, 1 volatile read, 1 branch,
    /// 1 array write, 1 volatile write. No CAS, no alloc, no signal, no lock.
    /// On overflow the new item is dropped (Dropped counter increments).
    /// </summary>
    public sealed class SpscBuffer<T> where T : class
    {
        private readonly T[] _buffer;
        private readonly int _mask;
        private long _head;
        private long _tail;
        private long _dropped;

        public long Received   { get { return Volatile.Read(ref _head); } }
        public long Dispatched { get { return Volatile.Read(ref _tail); } }
        public long Pending    { get { return Received - Dispatched; } }
        public long Dropped    { get { return Volatile.Read(ref _dropped); } }
        public int  Capacity   { get { return _buffer.Length; } }

        public SpscBuffer(int desiredCapacity)
        {
            int cap = 1;
            while (cap < desiredCapacity) cap <<= 1;
            _buffer = new T[cap];
            _mask = cap - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(T item)
        {
            long head = _head;
            long tail = Volatile.Read(ref _tail);
            if (head - tail >= _buffer.Length)
            {
                _dropped++;
                return false;
            }
            _buffer[head & _mask] = item;
            Volatile.Write(ref _head, head + 1);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            long tail = _tail;
            long head = Volatile.Read(ref _head);
            if (tail >= head)
            {
                item = null;
                return false;
            }
            item = _buffer[tail & _mask];
            _buffer[tail & _mask] = null;
            Volatile.Write(ref _tail, tail + 1);
            return true;
        }
    }
}
