using dc_event_consumer.Ipc;

namespace dc_antibot.AntiBot.Shared
{

    public sealed class CertSnapshot
    {
        public bool IsSigned { get; private set; }
        public bool IsMicrosoft { get; private set; }
        public DscResult Result { get; private set; }
        public int ChainCount { get; private set; }
        public string PrimaryThumbprint { get; private set; }
        public string PrimarySubject { get; private set; }
        public string PrimaryIssuer { get; private set; }
        public string PrimarySerial { get; private set; }

        public bool IsBlacklisted { get; internal set; }
        public bool IsChecked { get { return Result != DscResult.Unchecked; } }
        public bool IsValid { get { return Result == DscResult.Trusted; } }
        public bool IsRevoked { get { return Result == DscResult.Revoked; } }
        public bool IsExpired { get { return Result == DscResult.Expired; } }
        public bool IsUntrusted { get { return Result == DscResult.Untrusted; } }

        public static CertSnapshot From(CertificateInfo c)
        {
            if (c == null) return null;

            var s = new CertSnapshot
            {
                IsSigned = c.IsSigned,
                IsMicrosoft = c.IsMicrosoft,
                Result = c.Result,
                ChainCount = c.ChainCount,
            };

            if (c.Chain != null && c.Chain.Count > 0)
            {
                var e = c.Chain[0];
                if (e != null)
                {
                    s.PrimaryThumbprint = e.Thumbprint;
                    s.PrimarySubject = e.Subject;
                    s.PrimaryIssuer = e.Issuer;
                    s.PrimarySerial = e.Serial;
                }
            }
            return s;
        }

        public string SummaryTag()
        {
            if (Result == DscResult.Unchecked) return "?";
            string sig = IsSigned ? (IsMicrosoft ? "Signed/MS" : "Signed") : "Unsigned";
            if (IsBlacklisted) return "BLACKLISTED-cert";
            if (IsRevoked) return "Revoked";
            if (IsExpired) return "Expired";
            if (IsUntrusted) return "Untrusted";
            if (IsSigned && !IsValid) return "Invalid";
            return sig;
        }

        public bool IsBetterThan(CertSnapshot other)
        {
            if (other == null) return true;
            if (!other.IsChecked && IsChecked) return true;
            if (other.IsChecked && !IsChecked) return false;
            return other.ChainCount < ChainCount;
        }
    }
}
