using System;
using System.Collections.Generic;
using APIHook;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;
using dc_antibot.AntiBot.Shared;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Modules.ScreenCapture
{
    public class ScreenCaptureModule : ModuleBase
    {
        public override string Name { get { return "ScreenCapture"; } }

        private const int MaxWatched = 512;

        private static readonly HashSet<string> ScreenApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "acquirenextframe",                
            "printwindow",   
            "bitblt", "stretchblt", "maskblt", "plgblt",
            "getdc", "getwindowdc",            
            "createdc", "createdca", "createdcw",
            "createcompatibledc", "createcompatiblebitmap",
            "getdibits",                       
        };

        private static bool IsScreenCaptureApi(string api)
        {
            if (string.IsNullOrEmpty(api)) return false;
            return ScreenApis.Contains(api);
        }

        private const double ThrottleMs = 500;

        private ApiMonitor _monitor;
        private readonly object _lock = new object();
        private readonly HashSet<int> _watched = new HashSet<int>();
        private readonly Dictionary<string, DateTime> _lastEmit =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        protected override void OnStart()
        {
            _monitor = new ApiMonitor(ApiHookNative.Dir);
            _monitor.ApiCalled += OnApiCalled;
            EventBus.OnProcess += OnProcess;
        }

        protected override void OnStop()
        {
            EventBus.OnProcess -= OnProcess;
            if (_monitor != null)
            {
                try { _monitor.ApiCalled -= OnApiCalled; } catch { }
                try { _monitor.Dispose(); } catch { }
                _monitor = null;
            }
            lock (_lock) { _watched.Clear(); _lastEmit.Clear(); }
        }

        private void OnProcess(ProcessTraceData data)
        {
            if (!data.IsStart)
            {
                int pid = data.ProcessId;
                lock (_lock)
                {
                    _watched.Remove(pid);
                    string prefix = pid + ":";
                    var toRemove = new List<string>();
                    foreach (var k in _lastEmit.Keys)
                        if (k.StartsWith(prefix, StringComparison.Ordinal)) toRemove.Add(k);
                    foreach (var k in toRemove) _lastEmit.Remove(k);
                }
                return;
            }

            var ctx = ProcessContextStore.Get(data.ProcessId, data.ImageFileName, data.ProcessName);
            if (ctx == null || !ctx.IsAnalyzable) return;

            if (TrustEvaluator.IsSignedAndClean(ctx.Signature)) return;

            lock (_lock)
            {
                if (_watched.Count >= MaxWatched) return;
                if (!_watched.Add(data.ProcessId)) return;
            }

            try { _monitor.Watch(data.ProcessId); } catch { }
        }

        private void OnApiCalled(object sender, ApiEvent e)
        {
            string api = e.Api ?? "";

            if (!IsScreenCaptureApi(api)) return;

            string key = e.Pid + ":" + api;
            lock (_lock)
            {
                if (!_watched.Contains(e.Pid)) return;

                DateTime last;
                if (_lastEmit.TryGetValue(key, out last) &&
                    (DateTime.Now - last).TotalMilliseconds < ThrottleMs)
                    return;

                _lastEmit[key] = DateTime.Now;
            }

            string path = null;
            var ctx = ProcessContextStore.Get(e.Pid, null, e.ProcessName);
            if (ctx != null) path = ctx.Path;

            string reason = "Screen capture via " + api;
            if (!string.IsNullOrEmpty(e.Detail)) reason += " (" + e.Detail + ")";
            if (e.Count > 1) reason += " x" + e.Count + "/s";

            Emit(new DetectionResult
            {
                Pid = e.Pid,
                ProcessName = e.ProcessName,
                ProcessPath = path,
                Reason = reason,
                Details = {
                    { "api", api },
                    { "detail", e.Detail ?? "" },
                    { "count", e.Count },
                }
            });
        }
    }
}
