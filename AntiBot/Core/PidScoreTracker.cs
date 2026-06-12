using System.Collections.Generic;
using dc_antibot.AntiBot.Common;

namespace dc_antibot.AntiBot.Core
{
    public class PidScoreTracker
    {
        private readonly object _lock = new object();
        private float _score;
        private readonly List<string> _reasons = new List<string>();
        private readonly HashSet<string> _seenSignals = new HashSet<string>();
        private byte _alertedAt; 

        public string ModuleName { get; set; }
        public int Pid { get; set; }
        public string ProcessName { get; set; }

        public PidScoreTracker() { }

        public PidScoreTracker(string moduleName, int pid, string processName)
        {
            ModuleName = moduleName;
            Pid = pid;
            ProcessName = processName;
        }

        public byte Score
        {
            get
            {
                lock (_lock)
                {
                    int s = (int)_score;
                    if (s < 0) return 0;
                    if (s > 10) return 10;
                    return (byte)s;
                }
            }
        }

        public IList<string> Reasons
        {
            get
            {
                lock (_lock)
                {
                    return _reasons.ToArray();
                }
            }
        }

        public bool AddSignal(string signalKey, float points, string reason)
        {
            bool increased;
            float newTotal;
            string module, name;
            int pid;

            lock (_lock)
            {
                if (signalKey != null && _seenSignals.Contains(signalKey))
                    return false;

                if (signalKey != null) _seenSignals.Add(signalKey);

                float before = _score;
                _score += points;
                if (_score > 10) _score = 10;
                if (_score < 0) _score = 0;

                _reasons.Add(reason);

                increased = _score > before;
                newTotal = _score;
                module = ModuleName;
                name = ProcessName;
                pid = Pid;
            }

            byte capped = newTotal < 0 ? (byte)0 : (newTotal > 10 ? (byte)10 : (byte)newTotal);
            string finalReason = increased ? reason : reason + " [capped]";

            ScoreLogger.Log(module, pid, name, points, newTotal, finalReason);
            ScoreBus.Raise(module, pid, name, points, capped, finalReason);

            return true;
        }

        public bool ShouldAlert(byte threshold)
        {
            lock (_lock)
            {
                if (_score < threshold) return false;
                byte current = (byte)(_score > 10 ? 10 : (int)_score);
                if (current <= _alertedAt) return false;
                _alertedAt = current;
                return true;
            }
        }

        public string LastReason
        {
            get
            {
                lock (_lock)
                {
                    return _reasons.Count > 0 ? _reasons[_reasons.Count - 1] : null;
                }
            }
        }

        public string JoinedReasons
        {
            get
            {
                lock (_lock)
                {
                    return string.Join(" | ", _reasons.ToArray());
                }
            }
        }
    }
}
