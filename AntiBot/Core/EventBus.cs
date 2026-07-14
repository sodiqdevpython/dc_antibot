using System;
using System.Diagnostics;
using System.Threading;
using dc_event_consumer;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Core
{

    public static class EventBus
    {
        private const int QueueCapacity = 8192;

        private static int _started;
        private static readonly int OwnPid = SafeGetOwnPid();

        public static event Action<ProcessTraceData>   OnProcess;
        public static event Action<NetworkIOTraceData> OnNetworkIO;
        public static event Action<ImageLoadTraceData> OnImageLoad;

        private static EventPump<ProcessTraceData>   _processPump;
        private static EventPump<NetworkIOTraceData> _networkPump;
        private static EventPump<ImageLoadTraceData> _imagePump;

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

            _processPump = new EventPump<ProcessTraceData>(
                "Process", QueueCapacity, DispatchProcess, TracePools.Return);
            _networkPump = new EventPump<NetworkIOTraceData>(
                "NetworkIO", QueueCapacity, DispatchNetworkIO, TracePools.Return);
            _imagePump   = new EventPump<ImageLoadTraceData>(
                "ImageLoad", QueueCapacity, DispatchImageLoad, TracePools.Return);

            _processPump.Start();
            _networkPump.Start();
            _imagePump.Start();

            EventBindings.Process   += EnqueueProcess;
            EventBindings.NetworkIO += EnqueueNetworkIO;
            EventBindings.ImageLoad += EnqueueImageLoad;

            EventConsumer.Start();
        }

        public static void Stop()
        {
            if (Interlocked.CompareExchange(ref _started, 0, 1) != 1) return;

            EventConsumer.Stop();

            EventBindings.Process   -= EnqueueProcess;
            EventBindings.NetworkIO -= EnqueueNetworkIO;
            EventBindings.ImageLoad -= EnqueueImageLoad;

            if (_processPump != null) _processPump.Stop();
            if (_networkPump != null) _networkPump.Stop();
            if (_imagePump   != null) _imagePump.Stop();
        }

        private static void EnqueueProcess(ProcessTraceData data)
        {
            if (data == null) return;
            if (IsExcludedPid(data.ProcessId)) { TracePools.Return(data); return; }
            _processPump.Enqueue(data);
        }

        private static void EnqueueNetworkIO(NetworkIOTraceData data)
        {
            if (data == null) return;
            if (IsExcludedPid(data.ProcessId)) { TracePools.Return(data); return; }
            _networkPump.Enqueue(data);
        }

        private static void EnqueueImageLoad(ImageLoadTraceData data)
        {
            if (data == null) return;
            if (IsExcludedPid(data.ProcessId)) { TracePools.Return(data); return; }
            _imagePump.Enqueue(data);
        }

        private static void DispatchProcess(ProcessTraceData data)
        {
            var h = OnProcess;
            if (h != null) SafeInvoke(() => h(data));
        }

        private static void DispatchNetworkIO(NetworkIOTraceData data)
        {
            var h = OnNetworkIO;
            if (h != null) SafeInvoke(() => h(data));
        }

        private static void DispatchImageLoad(ImageLoadTraceData data)
        {
            var h = OnImageLoad;
            if (h != null) SafeInvoke(() => h(data));
        }

        private static void SafeInvoke(Action action)
        {
            try { action(); } catch { }
        }
    }
}
