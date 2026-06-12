using System;
using System.Collections.Generic;
using dc_antibot.AntiBot.Core;

namespace dc_antibot.AntiBot.Models
{
    public class DetectionResult
    {
        public string ModuleName { get; set; }
        public int Pid { get; set; }
        public string ProcessName { get; set; }
        public string ProcessPath { get; set; }
        public byte Score { get; set; }
        public Severity Severity { get; set; }
        public string Reason { get; set; }
        public DateTime DetectedAt { get; set; }
        public Dictionary<string, object> Details { get; set; }

        public DetectionResult()
        {
            DetectedAt = DateTime.Now;
            Details = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            return string.Format(
                "[{0}] [{1}] PID:{2} ({3}) Score:{4}/10 - {5}",
                Severity, ModuleName, Pid, ProcessName ?? "?", Score, Reason);
        }
    }
}
