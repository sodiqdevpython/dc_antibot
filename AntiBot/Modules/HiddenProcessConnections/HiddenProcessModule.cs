using System.Collections.Concurrent;
using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;
using dc_antibot.AntiBot.Shared;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Modules.HiddenProcessConnections
{
    public class HiddenProcessModule : ModuleBase
    {
        public override string Name { get { return "HiddenProcessConnections"; } }

        private readonly ConcurrentDictionary<int, HiddenState> _states =
            new ConcurrentDictionary<int, HiddenState>();

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
            HiddenState _;
            _states.TryRemove(data.ProcessId, out _);
        }

        private void OnNetwork(NetworkIOTraceData data)
        {
            if (!NetworkUtils.IsPublic(data.RemoteAddress)) return;

            if (!BlacklistStore.Contains(data.RemoteAddress)) return;

            var ctx = ProcessContextStore.Get(data.ProcessId, data.ProcessImagePath, data.ProcessName);
            if (ctx == null) return;
            if (!ctx.IsBackground) return;

            var state = _states.GetOrAdd(data.ProcessId, _ => new HiddenState());
            lock (state.AlertedIps)
            {
                if (!state.AlertedIps.Add(data.RemoteAddress)) return;
            }

            Emit(new DetectionResult
            {
                Pid = data.ProcessId,
                ProcessName = data.ProcessName,
                ProcessPath = data.ProcessImagePath,
                Score = 10,
                Reason = "Hidden process connected to blacklisted IP: " + data.RemoteAddress + ":" + data.RemotePort,
                Details = {
                    { "remote", data.RemoteAddress + ":" + data.RemotePort },
                    { "sessionId", (int)ctx.SessionId },
                    { "signature", ctx.SignatureSummary() },
                }
            });
        }
    }
}
