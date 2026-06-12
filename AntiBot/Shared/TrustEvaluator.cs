using dc_helper.digital_signer;

namespace dc_antibot.AntiBot.Shared
{

    public static class TrustEvaluator
    {
       
        private const float SignedMultiplier = 0.3f;

        public static bool ShouldSkip(SignatureInfo sig)
        {
            if (sig == null) return false;
            if (!sig.IsMicrosoft) return false;
            if (!sig.IsValid) return false;
            if (sig.IsRevoked) return false;
            if (sig.IsBlacklisted) return false;
            return true;
        }

        public static bool IsSignedAndClean(SignatureInfo sig)
        {
            if (sig == null) return false;
            return sig.IsSigned && sig.IsValid && !sig.IsRevoked && !sig.IsBlacklisted;
        }

        public static float ScoreMultiplier(SignatureInfo sig)
        {
            return IsSignedAndClean(sig) ? SignedMultiplier : 1.0f;
        }

        public static int RiskScore(SignatureInfo sig)
        {
            return sig == null ? 70 : sig.Score;
        }
    }
}
