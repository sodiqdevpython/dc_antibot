using System;
using System.Globalization;
using System.IO;

namespace dc_antibot.AntiBot.Common
{
    public static class ScoreLogger
    {
        private static readonly object _lock = new object();

        public static bool Enabled = false;

        public static string FileName = "score_log.txt";

        private static string FullPath
        {
            get
            {
                try { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName); }
                catch { return FileName; }
            }
        }

        public static void SessionStart()
        {
            if (!Enabled) return;
        }

        public static void Log(string module, int pid, string processName, float delta, float total, string reason)
        {
            if (!Enabled) return;

            string line = string.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff}  [{1,-24}] PID:{2,-6} {3,-26} +{4,-5} => {5,4}/10  | {6}",
                DateTime.Now,
                module ?? "?",
                pid,
                Trim(processName ?? "?", 26),
                delta.ToString("0.0", CultureInfo.InvariantCulture),
                total.ToString("0.0", CultureInfo.InvariantCulture),
                reason ?? "");

            Write(line);
        }

        public static void LogAggregate(int pid, string processName, float total, string reason)
        {
            if (!Enabled) return;

            string line = string.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff}  [{1,-24}] PID:{2,-6} {3,-26} ====> {4,4}/10  | {5}",
                DateTime.Now,
                "AggregateRisk",
                pid,
                Trim(processName ?? "?", 26),
                total.ToString("0.0", CultureInfo.InvariantCulture),
                reason ?? "");

            Write(line);
        }

        private static void Write(string text)
        {
            lock (_lock)
            {
                try { File.AppendAllText(FullPath, text + Environment.NewLine); }
                catch { }
            }
        }

        private static string Trim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
