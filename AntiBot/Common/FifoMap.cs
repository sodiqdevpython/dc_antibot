using System;
using System.Collections.Concurrent;

namespace dc_antibot.AntiBot.Common
{
    public class FifoMap<TValue> where TValue : class
    {
        private readonly int _cap;
        private readonly ConcurrentDictionary<string, TValue> _map;
        private readonly ConcurrentQueue<string> _order = new ConcurrentQueue<string>();

        public FifoMap(int capacity)
        {
            _cap = capacity < 1 ? 1 : capacity;
            _map = new ConcurrentDictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        }

        public int Count { get { return _map.Count; } }

        public TValue GetOrAdd(string key, Func<string, TValue> factory)
        {
            TValue v;
            if (_map.TryGetValue(key, out v)) return v;

            TValue created = factory(key);
            v = _map.GetOrAdd(key, created);
            if (object.ReferenceEquals(v, created))
            {
                _order.Enqueue(key);
                Evict();
            }
            return v;
        }

        public void Clear()
        {
            _map.Clear();
            string _;
            while (_order.TryDequeue(out _)) { }
        }

        private void Evict()
        {
            while (_map.Count > _cap)
            {
                string oldest;
                if (!_order.TryDequeue(out oldest)) break;
                TValue _;
                _map.TryRemove(oldest, out _);
            }
        }
    }
}
