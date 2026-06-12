using System;
using dc_helper;

namespace dc_antibot.AntiBot.Common
{
    public static class BlacklistStore
    {
        public static bool Contains(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            try { return ExclusionManager.IsExcluded(ip); }
            catch { return false; }
        }
    }
}
