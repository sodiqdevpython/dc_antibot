using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using dc_event_consumer;
using dc_event_consumer.Models;

namespace dc_antibot.AntiBot.Core
{

    public static class EventBus
    {
        private const int BufferSize = 16384;

        private static readonly SpscBuffer<ProcessTraceData>   _procBuf = new SpscBuffer<ProcessTraceData>(BufferSize);
        private static readonly SpscBuffer<NetworkIOTraceData> _netBuf  = new SpscBuffer<NetworkIOTraceData>(BufferSize);
        private static readonly SpscBuffer<ImageLoadTraceData> _imgBuf  = new SpscBuffer<ImageLoadTraceData>(BufferSize);

        private static Thread _procW, _netW, _imgW, _consumerT;
        private static volatile bool _running;
        private static readonly int OwnPid = SafeGetOwnPid();

        public static event Action<ProcessTraceData>   OnProcess;
        public static event Action<NetworkIOTraceData> OnNetworkIO;
        public static event Action<ImageLoadTraceData> OnImageLoad;

        public static bool IsStarted { get { return _running; } }

        public static long ProcessPending   { get { return _procBuf.Pending; } }
        public static long NetworkPending   { get { return _netBuf.Pending; } }
        public static long ImageLoadPending { get { return _imgBuf.Pending; } }

        public static long ProcessDropped   { get { return _procBuf.Dropped; } }
        public static long NetworkDropped   { get { return _netBuf.Dropped; } }
        public static long ImageLoadDropped { get { return _imgBuf.Dropped; } }

        public static void Start()
        {
            if (_running) return;
            _running = true;

            _procW = StartWorker("EventBus-Process",   () => Loop(_procBuf, DispatchProcess));
            _netW  = StartWorker("EventBus-NetworkIO", () => Loop(_netBuf,  DispatchNetworkIO));
            _imgW  = StartWorker("EventBus-ImageLoad", () => Loop(_imgBuf,  DispatchImageLoad));

            EventBindings.Process   += Publish;
            EventBindings.NetworkIO += Publish;
            EventBindings.ImageLoad += Publish;

            _consumerT = new Thread(() => EventConsumer.Start())
            {
                IsBackground = true,
                Name = "EventConsumer",
            };
            _consumerT.Start();
        }

        public static void Stop()
        {
            if (!_running) return;
            _running = false;

            try { EventConsumer.Stop(); } catch { }

            EventBindings.Process   -= Publish;
            EventBindings.NetworkIO -= Publish;
            EventBindings.ImageLoad -= Publish;

            try { if (_procW != null) _procW.Join(500); } catch { }
            try { if (_netW  != null) _netW.Join(500);  } catch { }
            try { if (_imgW  != null) _imgW.Join(500);  } catch { }
            try { if (_consumerT != null) _consumerT.Join(500); } catch { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Publish(ProcessTraceData d)
        {
            if (d == null || d.ProcessId <= 4 || d.ProcessId == OwnPid) return;
            _procBuf.TryEnqueue(d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Publish(NetworkIOTraceData d)
        {
            if (d == null || d.ProcessId <= 4 || d.ProcessId == OwnPid) return;
            _netBuf.TryEnqueue(d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Publish(ImageLoadTraceData d)
        {
            if (d == null || d.ProcessId <= 4 || d.ProcessId == OwnPid) return;
            _imgBuf.TryEnqueue(d);
        }

        private static void DispatchProcess(ProcessTraceData d)
        {
            var h = OnProcess;
            if (h != null) SafeInvoke(h, d);
        }

        private static void DispatchNetworkIO(NetworkIOTraceData d)
        {
            var h = OnNetworkIO;
            if (h != null) SafeInvoke(h, d);
        }

        private static void DispatchImageLoad(ImageLoadTraceData d)
        {
            var h = OnImageLoad;
            if (h != null) SafeInvoke(h, d);
        }

        private static void SafeInvoke<T>(Action<T> h, T d)
        {
            foreach (Action<T> single in h.GetInvocationList())
            {
                try { single(d); } catch { }
            }
        }

        private static Thread StartWorker(string name, ThreadStart body)
        {
            var t = new Thread(body)
            {
                IsBackground = true,
                Name = name,
                Priority = ThreadPriority.Highest,
            };
            t.Start();
            return t;
        }

        private static void Loop<T>(SpscBuffer<T> buf, Action<T> dispatch) where T : class
        {
            var sw = new SpinWait();
            T evt;
            while (_running)
            {
                if (buf.TryDequeue(out evt))
                {
                    dispatch(evt);
                    sw.Reset();
                    continue;
                }
                sw.SpinOnce();
            }
        }

        private static int SafeGetOwnPid()
        {
            try { return Process.GetCurrentProcess().Id; }
            catch { return -1; }
        }
    }
}
