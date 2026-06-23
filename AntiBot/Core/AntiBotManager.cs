using System;
using System.Collections.Generic;
using System.Linq;
using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Modules.C2Connections;
using dc_antibot.AntiBot.Modules.HiddenProcessConnections;
using dc_antibot.AntiBot.Modules.NetworkScanning;
using dc_antibot.AntiBot.Modules.NonStandardConnection;
using dc_antibot.AntiBot.Modules.ScreenCapture;

namespace dc_antibot.AntiBot.Core
{
    public class AntiBotManager
    {
        private readonly ModuleConfig _config;
        private readonly Dictionary<string, IDetectionModule> _all;

        public C2ConnectionsModule  C2            { get; private set; }
        public HiddenProcessModule  Hidden        { get; private set; }
        public NonStandardModule    NonStandard   { get; private set; }
        public NetworkScanModule    NetworkScan   { get; private set; }
        public ScreenCaptureModule  ScreenCapture { get; private set; }

        public bool IsRunning { get; private set; }

        public AntiBotManager(ModuleConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            _config = config;

            C2            = new C2ConnectionsModule();
            Hidden        = new HiddenProcessModule();
            NonStandard   = new NonStandardModule();
            NetworkScan   = new NetworkScanModule();
            ScreenCapture = new ScreenCaptureModule();

            _all = new Dictionary<string, IDetectionModule>(StringComparer.OrdinalIgnoreCase)
            {
                { C2.Name,            C2 },
                { Hidden.Name,        Hidden },
                { NonStandard.Name,   NonStandard },
                { NetworkScan.Name,   NetworkScan },
                { ScreenCapture.Name, ScreenCapture },
            };
        }

        public IEnumerable<string> ModuleNames
        {
            get { return _all.Keys.ToArray(); }
        }

        public bool IsModuleRunning(string name)
        {
            IDetectionModule m;
            return _all.TryGetValue(name, out m) && m.IsRunning;
        }

        public Dictionary<string, bool> GetState()
        {
            var dict = new Dictionary<string, bool>();
            foreach (var kv in _all) dict[kv.Key] = kv.Value.IsRunning;
            return dict;
        }

        public void Start()
        {
            if (IsRunning) return;

            ScoreLogger.SessionStart();
            EventBus.Start();

            if (_config.EnableC2Connections)            SafeStart(C2);
            if (_config.EnableHiddenProcessConnections) SafeStart(Hidden);
            if (_config.EnableNonStandardConnection)    SafeStart(NonStandard);
            if (_config.EnableNetworkScanning)          SafeStart(NetworkScan);
            if (_config.EnableScreenCapture)            SafeStart(ScreenCapture);

            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;

            foreach (var m in _all.Values)
            {
                try { m.Stop(); } catch { }
            }

            EventBus.Stop();
            IsRunning = false;
        }

        public bool StartModule(string name)
        {
            IDetectionModule m;
            if (!_all.TryGetValue(name, out m)) return false;
            if (m.IsRunning) return true;
            try { m.Start(); return true; } catch { return false; }
        }

        public bool StopModule(string name)
        {
            IDetectionModule m;
            if (!_all.TryGetValue(name, out m)) return false;
            if (!m.IsRunning) return true;
            try { m.Stop(); return true; } catch { return false; }
        }

        private static void SafeStart(IDetectionModule m)
        {
            try { m.Start(); }
            catch (Exception ex)
            {
                Console.WriteLine("[AntiBot] " + m.Name + " failed to start: " + ex.Message);
            }
        }
    }
}
