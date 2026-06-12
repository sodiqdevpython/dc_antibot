using System;
using System.Collections.Generic;
using System.IO;

namespace dc_antibot.AntiBot.Modules.NonStandardConnection
{
    internal static class PortAllowList
    {
        private static readonly Dictionary<string, HashSet<int>> _allowed =
            new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase)
        {
            { "outlook.exe", new HashSet<int> { 25, 110, 143, 465, 587, 993, 995 } },
            { "thunderbird.exe", new HashSet<int> { 25, 110, 143, 465, 587, 993, 995 } },
            { "ssh.exe",     new HashSet<int> { 22 } },
        };

        public static bool IsAllowed(string processPath, int port)
        {
            if (string.IsNullOrEmpty(processPath)) return false;
            string name;
            try { name = Path.GetFileName(processPath); }
            catch { return false; }
            if (string.IsNullOrEmpty(name)) return false;

            HashSet<int> ports;
            if (!_allowed.TryGetValue(name, out ports)) return false;
            return ports.Contains(port);
        }
    }
}
