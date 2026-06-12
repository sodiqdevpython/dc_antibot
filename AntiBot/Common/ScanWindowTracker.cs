using System.Collections.Generic;

namespace dc_antibot.AntiBot.Common
{
    public class ScanWindowTracker
    {
        private struct Entry
        {
            public string Ip;
            public int Port;
            public bool IsPrivateIp;
        }

        private readonly object _lock = new object();
        private readonly Queue<Entry> _entries = new Queue<Entry>();
        private readonly int _capacity;

        public ScanWindowTracker(int capacity)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public void Add(string ip, int port, bool isPrivateIp)
        {
            lock (_lock)
            {
                _entries.Enqueue(new Entry { Ip = ip, Port = port, IsPrivateIp = isPrivateIp });
                while (_entries.Count > _capacity)
                    _entries.Dequeue(); 
            }
        }

        public int MaxPortsPerIp(out string topIp)
        {
            topIp = null;
            lock (_lock)
            {
                var map = new Dictionary<string, HashSet<int>>();
                foreach (var e in _entries)
                {
                    if (string.IsNullOrEmpty(e.Ip)) continue;
                    HashSet<int> set;
                    if (!map.TryGetValue(e.Ip, out set)) { set = new HashSet<int>(); map[e.Ip] = set; }
                    set.Add(e.Port);
                }
                int max = 0;
                foreach (var kv in map)
                    if (kv.Value.Count > max) { max = kv.Value.Count; topIp = kv.Key; }
                return max;
            }
        }
        public int MaxIpsPerPort(out int topPort)
        {
            topPort = 0;
            lock (_lock)
            {
                var map = new Dictionary<int, HashSet<string>>();
                foreach (var e in _entries)
                {
                    if (PortClassifier.IsStandardWebPort(e.Port)) continue;
                    HashSet<string> set;
                    if (!map.TryGetValue(e.Port, out set)) { set = new HashSet<string>(); map[e.Port] = set; }
                    if (!string.IsNullOrEmpty(e.Ip)) set.Add(e.Ip);
                }
                int max = 0;
                foreach (var kv in map)
                    if (kv.Value.Count > max) { max = kv.Value.Count; topPort = kv.Key; }
                return max;
            }
        }

        public int UniquePrivateIps()
        {
            lock (_lock)
            {
                var set = new HashSet<string>();
                foreach (var e in _entries)
                    if (e.IsPrivateIp && !string.IsNullOrEmpty(e.Ip)) set.Add(e.Ip);
                return set.Count;
            }
        }
    }
}
