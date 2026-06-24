using System;
using System.IO;

namespace dc_antibot.AntiBot.Shared
{
    public static class ApiHookNative
    {
        public static string Dir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apihook"); }
        }
    }
}
