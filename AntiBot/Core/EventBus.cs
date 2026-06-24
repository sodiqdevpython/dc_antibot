using System;
using System.Diagnostics;
using System.Threading;
using dc_event_consumer.Bindings;
using dc_event_consumer.Core;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Core
{

    public static class EventBus
    {
        private static int _started; // 0 = stopped, 1 = started
        private static readonly int OwnPid = SafeGetOwnPid();

        public static event Action<ProcessTraceData>   OnProcess;
        public static event Action<NetworkIOTraceData> OnNetworkIO;
        public static event Action<ImageLoadTraceData> OnImageLoad;

        public static bool IsStarted { get { return _started == 1; } }

        private static bool IsExcludedPid(int pid)
        {
            return pid <= 4 || pid == OwnPid;
        }

        private static int SafeGetOwnPid()
        {
            try { return Process.GetCurrentProcess().Id; }
            catch { return -1; }
        }

        public static void Start()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;

            EventBindings.Process.OnEvent   += DispatchProcess;
            EventBindings.NetworkIO.OnEvent += DispatchNetworkIO;
            EventBindings.ImageLoad.OnEvent += DispatchImageLoad;

            EventConsumer.Start();
        }

        public static void Stop()
        {
            if (Interlocked.CompareExchange(ref _started, 0, 1) != 1) return;

            EventConsumer.Stop();

            EventBindings.Process.OnEvent   -= DispatchProcess;
            EventBindings.NetworkIO.OnEvent -= DispatchNetworkIO;
            EventBindings.ImageLoad.OnEvent -= DispatchImageLoad;
        }

        private static void DispatchProcess(ProcessTraceData data)
        {
            if (IsExcludedPid(data.ProcessId)) return;
            var h = OnProcess;
            if (h != null) SafeInvoke(() => h(data));
        }

        private static void DispatchNetworkIO(NetworkIOTraceData data)
        {
            if (IsExcludedPid(data.ProcessId)) return;
            var h = OnNetworkIO;
            if (h != null) SafeInvoke(() => h(data));
        }

        private static void DispatchImageLoad(ImageLoadTraceData data)
        {
            if (IsExcludedPid(data.ProcessId)) return;
            var h = OnImageLoad;
            if (h != null) SafeInvoke(() => h(data));
        }

        private static void SafeInvoke(Action action)
        {
            try { action(); } catch { /* dispatcher yiqilmasligi kerak */ }
        }
    }
}
