using System;
using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;
using dc_antibot.AntiBot.Shared;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Modules.C2Connections
{
    public class C2ConnectionsModule : ModuleBase
    {
        public override string Name { get { return "C2Connections"; } }

        public event EventHandler<ScoreUpEventArgs> OnScoreUp;

        private const byte AlertThreshold = 7;
        private const int  FifoSize       = 5000;

        private readonly FifoMap<PidScoreTracker> _trackers = new FifoMap<PidScoreTracker>(FifoSize);

        protected override void OnStart()
        {
            EventBus.OnNetworkIO += OnNetwork;
            ScoreBus.ScoreUp     += OnScoreUpFromBus;
        }

        protected override void OnStop()
        {
            EventBus.OnNetworkIO -= OnNetwork;
            ScoreBus.ScoreUp     -= OnScoreUpFromBus;
            _trackers.Clear();
        }

        private void OnScoreUpFromBus(ScoreUpEventArgs e)
        {
            if (!string.Equals(e.ModuleName, Name, StringComparison.OrdinalIgnoreCase)) return;
            var h = OnScoreUp;
            if (h == null) return;
            try { h(this, e); } catch { }
        }

        private void OnNetwork(NetworkIOTraceData data)
        {
            if (!NetworkUtils.IsPublic(data.RemoteAddress)) return;

            var ctx = ProcessContextStore.Get(data.ProcessId, data.ProcessImagePath, data.ProcessName, data.CertInfo);
            if (ctx == null) return;
            if (!ctx.IsAnalyzable) return;  

            bool isBlacklistedIp = BlacklistStore.Contains(data.RemoteAddress);

            if (isBlacklistedIp)
            {
                var bt = GetTracker(ctx, data);
                bt.AddSignal("blacklist:" + data.RemoteAddress, 10f,
                    "Connection to blacklisted IP: " + data.RemoteAddress);
                if (bt.ShouldAlert(AlertThreshold))
                    Emit(Build(data, ctx, bt));
                return;
            }

            if (ctx.ShouldSkip) return;

            if (!SharedSignals.MatchesGate(ctx)) return;

            var tracker = GetTracker(ctx, data);

            SharedSignals.ApplyGate(tracker, ctx, 5f);

            if (!PortClassifier.IsStandardWebPort(data.RemotePort))
            {
                if (PortClassifier.IsCommonC2Port(data.RemotePort))
                    tracker.AddSignal("port:c2", 3f,
                        "Known C2/RAT port: " + data.RemoteAddress + ":" + data.RemotePort);
                else
                    tracker.AddSignal("port:unusual", 2f,
                        "Unusual remote port: " + data.RemotePort);
            }

            SharedSignals.ApplyAutorun(tracker, ctx, 3f);
            SharedSignals.ApplyLateAutorun(tracker, ctx, 2.5f);
            SharedSignals.ApplyServiceSession(tracker, ctx, 2.5f);
            SharedSignals.ApplyAvExclusion(tracker, ctx, 4f);
            SharedSignals.ApplyUnusualPath(tracker, ctx);
            SharedSignals.ApplyNetworkIat(tracker, ctx);
            SharedSignals.ApplySuspiciousApis(tracker, ctx);

            SharedSignals.ApplyBeacon(tracker, ctx, data.RemoteAddress, 2.5f);
            SharedSignals.ApplyTraffic(tracker, ctx, data.IsSend, data.Size);

            if (tracker.ShouldAlert(AlertThreshold))
                Emit(Build(data, ctx, tracker));
        }

        private PidScoreTracker GetTracker(ProcessContext ctx, NetworkIOTraceData data)
        {
            string key = PathKey.For(data.ProcessId, ctx.Path);
            var tracker = _trackers.GetOrAdd(key,
                _ => new PidScoreTracker(Name, data.ProcessId, data.ProcessName));
            tracker.Pid = data.ProcessId;
            tracker.ProcessName = data.ProcessName;
            return tracker;
        }

        private DetectionResult Build(NetworkIOTraceData data, ProcessContext ctx, PidScoreTracker tracker)
        {
            return new DetectionResult
            {
                Pid = data.ProcessId,
                ProcessName = data.ProcessName,
                ProcessPath = data.ProcessImagePath,
                Score = tracker.Score,
                Reason = tracker.JoinedReasons,
                Details = {
                    { "remote", data.RemoteAddress + ":" + data.RemotePort },
                    { "signature", ctx.SignatureSummary() },
                    { "autorun", ctx.AutorunLocation ?? "-" },
                    { "avExclusion", ctx.IsInAvExclusion ? (ctx.AvExclusionMatch ?? "yes") : "-" },
                    { "iat", ctx.NetworkImports.Summary() },
                }
            };
        }
    }
}
