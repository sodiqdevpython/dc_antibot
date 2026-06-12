using System;

namespace dc_antibot.AntiBot.Models
{
    public class ScoreUpEventArgs : EventArgs
    {
        public string ModuleName { get; set; }
        public int Pid { get; set; }
        public string ProcessName { get; set; }
        public float Delta { get; set; }
        public byte Score { get; set; }
        public string Reason { get; set; }
        public DateTime At { get; set; }
    }
}
