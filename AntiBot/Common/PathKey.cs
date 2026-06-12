namespace dc_antibot.AntiBot.Common
{
    public static class PathKey
    {
        public static string For(int pid, string path)
        {
            if (!string.IsNullOrEmpty(path)) return path.ToLowerInvariant();
            return "pid:" + pid;
        }
    }
}
