using System;
using System.Collections.Generic;

namespace dc_antibot.AntiBot.Shared
{
    public class BeaconTracker
    {
        private const int    MinSamples     = 4;        
        private const int    KeepSamples    = 8;        
        private const double MinIntervalSec = 5.0;     
        private const double MaxIntervalSec = 3600.0; 
        private const double MaxCV          = 0.25;    
        private const int    MaxIps         = 64;

        private class Track
        {
            public readonly Queue<DateTime> Times = new Queue<DateTime>();
            public DateTime Last;
            public bool Flagged;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<string, Track> _byIp = new Dictionary<string, Track>();
        private readonly Queue<string> _order = new Queue<string>();

        public string OnConnection(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return null;
            DateTime now = DateTime.Now;

            lock (_lock)
            {
                Track t;
                if (!_byIp.TryGetValue(ip, out t))
                {
                    if (_order.Count >= MaxIps)
                    {
                        string old = _order.Dequeue();
                        _byIp.Remove(old);
                    }
                    t = new Track();
                    _byIp[ip] = t;
                    _order.Enqueue(ip);
                    t.Last = now;
                    t.Times.Enqueue(now);
                    return null;
                }

                if ((now - t.Last).TotalSeconds < MinIntervalSec) return null;

                t.Last = now;
                t.Times.Enqueue(now);
                while (t.Times.Count > KeepSamples) t.Times.Dequeue();

                if (t.Flagged) return null;
                if (t.Times.Count < MinSamples) return null;

                DateTime[] arr = t.Times.ToArray();
                int n = arr.Length - 1;
                double sum = 0;
                double[] gaps = new double[n];
                for (int i = 1; i < arr.Length; i++)
                {
                    gaps[i - 1] = (arr[i] - arr[i - 1]).TotalSeconds;
                    sum += gaps[i - 1];
                }
                double mean = sum / n;
                if (mean < MinIntervalSec || mean > MaxIntervalSec) return null;

                double varSum = 0;
                for (int i = 0; i < n; i++)
                {
                    double d = gaps[i] - mean;
                    varSum += d * d;
                }
                double std = Math.Sqrt(varSum / n);
                double cv = mean > 0 ? std / mean : 1.0;

                if (cv <= MaxCV)
                {
                    t.Flagged = true;
                    return ip + " (~" + Math.Round(mean) + "s interval, jitter " + Math.Round(cv * 100) + "%)";
                }
                return null;
            }
        }
    }
}
