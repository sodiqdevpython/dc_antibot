using System;
using System.Collections.Generic;

namespace dc_antibot.AntiBot.Models
{
    public class NonStandardState
    {
        public readonly HashSet<string> AlertedProtocols
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> SeenProtocols
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
