using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Core;

namespace dc_antibot.AntiBot.Shared
{
    public static class SharedSignals
    {
        public static void ApplySignature(PidScoreTracker tracker, ProcessContext ctx)
        {
            if (tracker == null || ctx == null || !ctx.IsAnalyzable) return;

            var c = ctx.Cert;
            if (c == null || !c.IsChecked) return;

            if (c.IsBlacklisted)
                tracker.AddSignal("sig:blacklist", 6f,
                    "Certificate blacklisted (" + (c.PrimarySubject ?? "?") + ")");
            else if (c.IsRevoked)
                tracker.AddSignal("sig:revoked", 5f, "Certificate revoked");
            else if (c.IsSigned && !c.IsValid)
                tracker.AddSignal("sig:invalid", 4f, "Invalid signature (" + c.Result + ")");
            else if (!c.IsSigned)
                tracker.AddSignal("sig:unsigned", 3f, "Unsigned binary");
        }

        public static void ApplyAutorun(PidScoreTracker tracker, ProcessContext ctx, float points)
        {
            if (tracker == null || ctx == null || !ctx.IsAnalyzable) return;
            string loc = ctx.AutorunLocation;
            if (loc == null) return;
            tracker.AddSignal("ctx:autorun", points * ctx.ScoreMultiplier,
                "Persistence via autorun: " + loc);
        }

        public static void ApplyAvExclusion(PidScoreTracker tracker, ProcessContext ctx, float points)
        {
            if (tracker == null || ctx == null || !ctx.IsAnalyzable) return;
            if (!ctx.IsInAvExclusion) return;
            tracker.AddSignal("ctx:avexcl", points * ctx.ScoreMultiplier,
                "Listed in AV exclusion: " + (ctx.AvExclusionMatch ?? "?"));
        }

        public static bool MatchesGate(ProcessContext ctx)
        {
            if (ctx == null || !ctx.IsAnalyzable) return false;

            if (!ctx.IsBackground) return false;

            var c = ctx.Cert;
            bool sigBad = (c == null)
                || !c.IsSigned
                || !c.IsValid
                || c.IsRevoked
                || c.IsBlacklisted;
            if (!sigBad) return false;

            return true;
        }

        public static void ApplyGate(PidScoreTracker tracker, ProcessContext ctx, float points)
        {
            if (tracker == null || ctx == null) return;
            tracker.AddSignal("gate", points,
                "Suspicious pattern: background + unsigned (" + ctx.SignatureSummary() + ")");
        }

        public static void ApplyBackground(PidScoreTracker tracker, ProcessContext ctx, float points)
        {
            if (tracker == null || ctx == null) return;
            if (!ctx.IsBackground) return;
            tracker.AddSignal("ctx:background", points * ctx.ScoreMultiplier,
                "Running in background (no visible window)");
        }

        public static void ApplyNetworkIat(PidScoreTracker tracker, ProcessContext ctx)
        {
            if (tracker == null || ctx == null) return;
            var p = ctx.NetworkImports;
            if (p == null || !p.HasAny) return;
            float mul = ctx.ScoreMultiplier;

            if (p.UsesUrlMon)
                tracker.AddSignal("iat:urlmon", 3f * mul,
                    "IAT: URLDownloadToFile (downloader pattern)");

            if (p.UsesTcpTableEnum)
                tracker.AddSignal("iat:tcptable", 1f * mul,
                    "IAT: GetExtendedTcpTable (recon)");
        }

        public static void ApplySuspiciousApis(PidScoreTracker tracker, ProcessContext ctx)
        {
            if (tracker == null || ctx == null || !ctx.IsAnalyzable) return;
            var p = ctx.SuspiciousApis;
            if (p == null || !p.HasAny) return;
            float mul = ctx.ScoreMultiplier;

            if (p.InjectionHits > 0)
                tracker.AddSignal("iat:injection", 3f * mul,
                    "IAT: Process Injection API (" + p.InjectionHits + " hits)");
            if (p.AntiDebugHits > 0)
                tracker.AddSignal("iat:antidebug", 1.5f * mul,
                    "IAT: Anti-Debug API (" + p.AntiDebugHits + " hits)");
            if (p.CryptoHits > 0)
                tracker.AddSignal("iat:crypto", 1f * mul,
                    "IAT: Crypto API (" + p.CryptoHits + " hits) - possible ransomware");
        }

        public static void ApplyUnusualPath(PidScoreTracker tracker, ProcessContext ctx)
        {
            if (tracker == null || ctx == null || !ctx.IsAnalyzable) return;
            float mul = ctx.ScoreMultiplier;
            switch (ctx.UnusualPath)
            {
                case UnusualPathLevel.Dangerous:
                    tracker.AddSignal("ctx:path-dangerous", 2.5f * mul,
                        "Launched from dangerous location (Desktop/Downloads/Temp): " + (ctx.Path ?? "?"));
                    break;
                case UnusualPathLevel.Suspicious:
                    tracker.AddSignal("ctx:path-suspicious", 1.5f * mul,
                        "Launched from suspicious location: " + (ctx.Path ?? "?"));
                    break;
            }
        }

        public static void ApplyLateAutorun(PidScoreTracker tracker, ProcessContext ctx, float points)
        {
            if (tracker == null || ctx == null || !ctx.IsAnalyzable) return;
            if (!ctx.TryRunLateAutorunCheck()) return;
            tracker.AddSignal("ctx:autorun-late", points * ctx.ScoreMultiplier,
                "Late persistence detected (added after process start): " + (ctx.AutorunLocation ?? "?"));
        }

        public static void ApplyServiceSession(PidScoreTracker tracker, ProcessContext ctx, float points)
        {
            if (tracker == null || ctx == null) return;
            if (!ctx.IsServiceSession) return;
            tracker.AddSignal("ctx:service-session", points,
                "Running in service/system context (Session 0)");
        }

        public static void ApplyBeacon(PidScoreTracker tracker, ProcessContext ctx, string ip, float points)
        {
            if (tracker == null || ctx == null) return;
            string desc = ctx.Beacon.OnConnection(ip);
            if (desc == null) return;
            tracker.AddSignal("beacon:" + ip, points * ctx.ScoreMultiplier,
                "Periodic beacon (C2 pattern): " + desc);
        }

        public static void ApplyTraffic(PidScoreTracker tracker, ProcessContext ctx, bool isSend, int size)
        {
            if (tracker == null || ctx == null) return;
            ctx.Traffic.OnSendOrRecv(isSend, size);

            var t = ctx.Traffic;
            float mul = ctx.ScoreMultiplier;

            if (t.TryRatioHit())
                tracker.AddSignal("traffic:ratio:" + t.RatioHits, 0.5f * mul,
                    "Sent/Received ratio >90% (" + t.RatioHits + "/5) - exfiltration indicator");

            if (t.TryMassiveExfil())
                tracker.AddSignal("traffic:massive", 5f * mul,
                    "Massive outbound transfer (>5GB total sent)");
        }
    }
}
