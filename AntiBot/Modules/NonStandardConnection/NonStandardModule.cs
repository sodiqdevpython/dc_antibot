using System.Collections.Concurrent;
using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;
using dc_antibot.AntiBot.Shared;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Modules.NonStandardConnection
{
    public class NonStandardModule : ModuleBase
    {
        public override string Name { get { return "NonStandardConnection"; } }

        private readonly ConcurrentDictionary<int, NonStandardState> _states =
            new ConcurrentDictionary<int, NonStandardState>();

        protected override void OnStart()
        {
            EventBus.OnProcess   += OnProcess;
            EventBus.OnNetworkIO += OnNetwork;
        }

        protected override void OnStop()
        {
            EventBus.OnProcess   -= OnProcess;
            EventBus.OnNetworkIO -= OnNetwork;
            _states.Clear();
        }

        private void OnProcess(ProcessTraceData data)
        {
            if (data.IsStart) return;
            NonStandardState _;
            _states.TryRemove(data.ProcessId, out _);
        }

        private void OnNetwork(NetworkIOTraceData data)
        {
            if (NetworkUtils.IsLoopback(data.RemoteAddress)) return;

            string proto;
            if (!PortClassifier.IsWatchedProtocol(data.RemotePort, out proto)) return;

            var ctx = ProcessContextStore.Get(data.ProcessId, data.ProcessImagePath, data.ProcessName);
            if (ctx == null) return;

            if (ctx.ShouldSkip) return;

            if (PortAllowList.IsAllowed(ctx.Path, data.RemotePort) && IsSignedClean(ctx))
                return;

            var state = _states.GetOrAdd(data.ProcessId, _ => new NonStandardState());

            bool firstProto;
            int distinctProtos;
            lock (state.AlertedProtocols)
            {
                firstProto = state.AlertedProtocols.Add(proto);
                state.SeenProtocols.Add(proto);
                distinctProtos = state.SeenProtocols.Count;
            }
            if (!firstProto && distinctProtos < 3) return;

            bool tunnelLike = distinctProtos >= 3;
            byte score = tunnelLike ? (byte)9 : (byte)7;
            string reason = tunnelLike
                ? ("Process used " + distinctProtos + " distinct non-standard protocols (tunneling indicator); latest: " + proto)
                : ("Non-standard port: " + proto + " (" + data.RemoteAddress + ":" + data.RemotePort + ")");

            Emit(new DetectionResult
            {
                Pid = data.ProcessId,
                ProcessName = data.ProcessName,
                ProcessPath = data.ProcessImagePath,
                Score = score,
                Reason = reason,
                Details = {
                    { "protocol", proto },
                    { "remote", data.RemoteAddress + ":" + data.RemotePort },
                    { "signature", ctx.SignatureSummary() },
                    { "background", ctx.IsBackground },
                }
            });
        }

        private static bool IsSignedClean(ProcessContext ctx)
        {
            return TrustEvaluator.IsSignedAndClean(ctx.Signature);
        }
    }
}
