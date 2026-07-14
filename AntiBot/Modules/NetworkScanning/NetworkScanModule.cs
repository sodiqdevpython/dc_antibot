using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;
using dc_antibot.AntiBot.Shared;
using dc_event_consumer.Models;
using dc_helper.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace dc_antibot.AntiBot.Modules.NetworkScanning
{
    public class NetworkScanModule : ModuleBase
    {
        public override string Name { get { return "NetworkScanning"; } }

        private const int FifoCapacity = 512;
        private const int PortScanThreshold  = 15;
        private const int SweepScanThreshold = 30;
        private const int LanScanThreshold   = 20;

        private readonly ConcurrentDictionary<int, ScanState> _states =
            new ConcurrentDictionary<int, ScanState>();

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
            ScanState _;
            _states.TryRemove(data.ProcessId, out _);
        }

        private void OnNetwork(NetworkIOTraceData data)
        {
            if (NetworkUtils.IsLoopback(data.RemoteAddress)) return;
            bool isPrivate = NetworkUtils.IsPrivate(data.RemoteAddress);

            var ctx = ProcessContextStore.Get(data.ProcessId, data.ProcessImagePath, data.ProcessName);
            if (ctx == null) return;
            if (ctx.ShouldSkip) return;

            var state = _states.GetOrAdd(data.ProcessId, _ => new ScanState(FifoCapacity));
            state.Window.Add(data.RemoteAddress, data.RemotePort, isPrivate);

            string topIp;
            int maxPorts = state.Window.MaxPortsPerIp(out topIp);
            if (maxPorts >= PortScanThreshold && TryFlag(state, "portscan:" + topIp))
            {
                Emit(new DetectionResult
                {
                    Pid = data.ProcessId,
                    ProcessName = data.ProcessName,
                    ProcessPath = data.ProcessImagePath,
                    Score = 8,
                    Reason = "Port scan: " + topIp + " (" + maxPorts + " unique ports in last " + FifoCapacity + " connections)",
                    Details = {
                        { "type", "port-scan" },
                        { "targetIp", topIp },
                        { "uniquePorts", maxPorts },
                    }
                });
            }

            int topPort;
            int maxIps = state.Window.MaxIpsPerPort(out topPort);
            if (maxIps >= SweepScanThreshold && TryFlag(state, "sweep:" + topPort))
            {
                Emit(new DetectionResult
                {
                    Pid = data.ProcessId,
                    ProcessName = data.ProcessName,
                    ProcessPath = data.ProcessImagePath,
                    Score = 8,
                    Reason = "Sweep scan: port " + topPort + " (" + maxIps + " unique IP)",
                    Details = {
                        { "type", "sweep-scan" },
                        { "targetPort", topPort },
                        { "uniqueIps", maxIps },
                    }
                });
            }

            int lanIps = state.Window.UniquePrivateIps();
            if (lanIps >= LanScanThreshold && TryFlag(state, "lanscan"))
            {
                Emit(new DetectionResult
                {
                    Pid = data.ProcessId,
                    ProcessName = data.ProcessName,
                    ProcessPath = data.ProcessImagePath,
                    Score = 6,
                    Reason = "LAN scan: " + lanIps + " distinct private IPs",
                    Details = {
                        { "type", "lan-scan" },
                        { "uniquePrivateIps", lanIps },
                    },
                    MitreAttacks = new List<MitreAttack>
                    {
                        new MitreAttack
                        {
                            TechniqueId = "T1046",
                            TechniqueName = "Network Service Discovery",
                            Tactic = "Discovery"
                        }
                    },
                });
            }
        }

        private static bool TryFlag(ScanState state, string key)
        {
            lock (state.AlertedTriggers) return state.AlertedTriggers.Add(key);
        }
    }
}
