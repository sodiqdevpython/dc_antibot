using dc_antibot.AntiBot.Common;

namespace dc_antibot.AntiBot.Shared
{
    public static class ProcessContextStore
    {
        private const int MaxEntries = 4096;
        private static readonly FifoMap<ProcessContext> _map = new FifoMap<ProcessContext>(MaxEntries);

        public static ProcessContext Get(int pid, string knownPath, string knownName)
        {
            if (pid <= 0) return null;
            string key = PathKey.For(pid, knownPath);
            var ctx = _map.GetOrAdd(key, _ => new ProcessContext(pid, knownPath, knownName));
            ctx.EnrichPath(knownPath, knownName);
            ctx.NotePid(pid); 
            return ctx;
        }

        public static void Clear()
        {
            _map.Clear();
        }
    }
}
