using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;
using dc_antibot.AntiBot.Shared;
using dc_event_consumer.Models;
using Microsoft.Win32;

namespace dc_antibot.AntiBot.Modules.Microphone
{
    public class MicrophoneModule : ModuleBase
    {
        public override string Name { get { return "Microphone"; } }

        private const int FlushDelayMs = 500;

        private static readonly HashSet<string> RecordDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mmdevapi.dll",
            "audioses.dll",
            "winmm.dll",
        };

        private const string ConsentStorePath =
            @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone\NonPackaged";

        private class PendingAlert
        {
            public int Pid;
            public string Path;
            public string ProcessName;
            public readonly HashSet<string> Dlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Timer Timer;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<int, PendingAlert> _pending = new Dictionary<int, PendingAlert>();

        protected override void OnStart()
        {
            EventBus.OnImageLoad += OnImageLoad;
            EventBus.OnProcess   += OnProcess;
        }

        protected override void OnStop()
        {
            EventBus.OnImageLoad -= OnImageLoad;
            EventBus.OnProcess   -= OnProcess;

            lock (_lock)
            {
                foreach (var p in _pending.Values)
                {
                    try { p.Timer.Dispose(); } catch { }
                }
                _pending.Clear();
            }
        }

        private void OnProcess(ProcessTraceData data)
        {
            if (data.IsStart) return;
            PendingAlert removed = null;
            lock (_lock)
            {
                if (_pending.TryGetValue(data.ProcessId, out removed))
                    _pending.Remove(data.ProcessId);
            }
            if (removed != null) { try { removed.Timer.Dispose(); } catch { } }
        }

        private void OnImageLoad(ImageLoadTraceData data)
        {
            if (!data.IsLoad) return;

            string file = SafeFileName(data.FileName);
            if (string.IsNullOrEmpty(file)) return;
            if (!RecordDlls.Contains(file)) return;

            int pid = data.ProcessId;
            string path = data.ProcessImagePath;

            var ctx = ProcessContextStore.Get(pid, path, data.ProcessName, data.CertInfo);
            if (ctx == null || !ctx.IsAnalyzable) return;

            if (TrustEvaluator.IsSignedAndClean(ctx.Cert)) return;

            if (!HasMicrophoneConsent(ctx.Path)) return;

            var c = ctx.Cert;
            bool blacklisted = (c != null && c.IsBlacklisted);
            if (!blacklisted && !ctx.IsBackground) return;

            lock (_lock)
            {
                PendingAlert pa;
                if (_pending.TryGetValue(pid, out pa))
                {
                    pa.Dlls.Add(file);
                    return;
                }

                pa = new PendingAlert
                {
                    Pid = pid,
                    Path = ctx.Path,
                    ProcessName = ctx.Name,
                };
                pa.Dlls.Add(file);
                pa.Timer = new Timer(Flush, pid, FlushDelayMs, Timeout.Infinite);
                _pending[pid] = pa;
            }
        }

        private void Flush(object state)
        {
            int pid = (int)state;
            PendingAlert pa;
            lock (_lock)
            {
                if (!_pending.TryGetValue(pid, out pa)) return;
                _pending.Remove(pid);
            }
            try { pa.Timer.Dispose(); } catch { }

            string dlls = string.Join(", ", pa.Dlls.ToArray());
            string reason = "Microphone access via " + dlls;

            Emit(new DetectionResult
            {
                Pid = pa.Pid,
                ProcessName = pa.ProcessName,
                ProcessPath = pa.Path,
                Reason = reason,
                Details = {
                    { "dlls", dlls },
                    { "count", pa.Dlls.Count },
                }
            });
        }

        private static bool HasMicrophoneConsent(string processPath)
        {
            if (string.IsNullOrEmpty(processPath)) return false;

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var root = baseKey.OpenSubKey(ConsentStorePath))
                {
                    if (CheckConsentInKey(root, processPath))
                        return true;
                }
            }
            catch { }

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
                {
                    foreach (string sid in baseKey.GetSubKeyNames())
                    {
                        if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase) ||
                            sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string fullPath = $@"{sid}\{ConsentStorePath}";

                        using (var root = baseKey.OpenSubKey(fullPath))
                        {
                            if (CheckConsentInKey(root, processPath))
                                return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool CheckConsentInKey(RegistryKey root, string processPath)
        {
            if (root == null) return false;

            foreach (var appName in root.GetSubKeyNames())
            {
                string exePath = appName.Replace('#', '\\');
                if (string.Equals(exePath, processPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string SafeFileName(string p)
        {
            if (string.IsNullOrEmpty(p)) return null;
            try { return Path.GetFileName(p).ToLowerInvariant(); }
            catch { return null; }
        }
    }
}
