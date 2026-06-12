namespace dc_antibot.AntiBot.Common
{
    public enum UnusualPathLevel
    {
        Normal = 0,
        Suspicious = 1,
        Dangerous = 2,
    }

    public static class PathClassifier
    {
        public static UnusualPathLevel Classify(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return UnusualPathLevel.Normal;
            string p = filePath.ToLowerInvariant();

            // Dangerous — foydalanuvchi yuklab darrov ishga tushira oladigan joylar
            if (p.Contains("\\appdata\\local\\temp\\")) return UnusualPathLevel.Dangerous;
            if (p.Contains("\\downloads\\"))           return UnusualPathLevel.Dangerous;
            if (p.Contains("\\desktop\\"))             return UnusualPathLevel.Dangerous;

            // Suspicious — qonuniy dastur deyarli ishlatmaydigan joylar
            if (p.Contains("\\$recycle.bin\\")) return UnusualPathLevel.Suspicious;
            if (p.Contains("\\windows\\temp\\")) return UnusualPathLevel.Suspicious;

            return UnusualPathLevel.Normal;
        }

        public static bool IsUnusual(string filePath)
        {
            return Classify(filePath) != UnusualPathLevel.Normal;
        }
    }
}
