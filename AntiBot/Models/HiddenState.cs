using System;
using System.Collections.Generic;

namespace dc_antibot.AntiBot.Models
{
    public class HiddenState
    {
        public readonly HashSet<string> AlertedIps
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
