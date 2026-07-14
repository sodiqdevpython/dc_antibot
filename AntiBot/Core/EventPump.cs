using System;
using System.Collections.Concurrent;
using System.Threading;

namespace dc_antibot.AntiBot.Core
{

    internal sealed class EventPump<T> where T : class
    {
        private readonly int _cap;
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, int.MaxValue);
        private readonly Action<T> _dispatch;
        private readonly Action<T> _release;
        private readonly string _name;
        private volatile bool _running;
        private int _count;
        private Thread _worker;

        public EventPump(string name, int capacity, Action<T> dispatch, Action<T> release)
        {
            _name = name;
            _cap = capacity < 1 ? 1 : capacity;
            _dispatch = dispatch ?? (_ => { });
            _release = release ?? (_ => { });
        }

        public int Count { get { return Volatile.Read(ref _count); } }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _worker = new Thread(Run)
            {
                IsBackground = true,
                Name = "EventPump:" + _name,
            };
            _worker.Start();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            try { _signal.Release(); } catch { }
            try { if (_worker != null) _worker.Join(2000); } catch { }

            T item;
            while (_queue.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _count);
                try { _release(item); } catch { }
            }
        }

        public void Enqueue(T item)
        {
            if (item == null) return;

            while (Volatile.Read(ref _count) >= _cap)
            {
                T old;
                if (!_queue.TryDequeue(out old)) break;
                Interlocked.Decrement(ref _count);
                try { _release(old); } catch { }
            }

            _queue.Enqueue(item);
            Interlocked.Increment(ref _count);
            try { _signal.Release(); } catch { }
        }

        private void Run()
        {
            while (_running)
            {
                try { _signal.Wait(250); } catch { }

                T item;
                while (_queue.TryDequeue(out item))
                {
                    Interlocked.Decrement(ref _count);
                    try { _dispatch(item); } catch { }
                    try { _release(item); } catch { }
                }
            }
        }
    }
}
