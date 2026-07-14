using dc_event_consumer.Ipc;

namespace dc_antibot.AntiBot.Shared
{

    public static class TrustEvaluator
    {
        private const float SignedMultiplier = 0.3f;

        public static bool ShouldSkip(CertSnapshot c)
        {
            if (c == null) return false;
            if (!c.IsMicrosoft) return false;
            if (!c.IsValid) return false;
            if (c.IsRevoked) return false;
            if (c.IsBlacklisted) return false;
            return true;
        }

        public static bool IsSignedAndClean(CertSnapshot c)
        {
            if (c == null) return false;
            return c.IsSigned && c.IsValid && !c.IsRevoked && !c.IsBlacklisted;
        }

        public static float ScoreMultiplier(CertSnapshot c)
        {
            return IsSignedAndClean(c) ? SignedMultiplier : 1.0f;
        }

        public static int RiskScore(CertSnapshot c)
        {
            if (c == null || !c.IsChecked) return 70;
            if (c.IsBlacklisted) return 95;
            if (c.IsRevoked) return 95;
            if (!c.IsSigned) return 80;
            if (c.IsUntrusted) return 75;
            if (c.IsExpired) return 60;
            if (!c.IsValid) return 70;
            return c.IsMicrosoft ? 5 : 20;
        }
    }
}
