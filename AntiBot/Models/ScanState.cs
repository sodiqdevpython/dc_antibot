using System;
using System.Collections.Generic;
using dc_antibot.AntiBot.Common;

namespace dc_antibot.AntiBot.Models
{
    public class ScanState
    {
        public readonly ScanWindowTracker Window;
        public readonly HashSet<string> AlertedTriggers
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ScanState(int capacity)
        {
            Window = new ScanWindowTracker(capacity);
        }
    }
}
