using System;
using dc_antibot.AntiBot.Models;

namespace dc_antibot.AntiBot.Core
{
    public static class ScoreBus
    {
        public static event Action<ScoreUpEventArgs> ScoreUp;

        public static void Raise(string module, int pid, string name, float delta, byte total, string reason)
        {
            var h = ScoreUp;
            if (h == null) return;
            try
            {
                h(new ScoreUpEventArgs
                {
                    ModuleName = module,
                    Pid = pid,
                    ProcessName = name,
                    Delta = delta,
                    Score = total,
                    Reason = reason,
                    At = DateTime.Now
                });
            }
            catch { }
        }
    }
}
