using System;
using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Modules.C2Connections;
using dc_antibot.AntiBot.Modules.HiddenProcessConnections;
using dc_antibot.AntiBot.Modules.Microphone;
using dc_antibot.AntiBot.Modules.NetworkScanning;
using dc_antibot.AntiBot.Modules.NonStandardConnection;

namespace dc_antibot.AntiBot.Core
{
    public class AntiBotManager
    {
        public C2ConnectionsModule  C2          { get; private set; }
        public HiddenProcessModule  Hidden      { get; private set; }
        public NonStandardModule    NonStandard { get; private set; }
        public NetworkScanModule    NetworkScan { get; private set; }
        public MicrophoneModule     Microphone  { get; private set; }

        public AntiBotManager()
        {
            ScoreLogger.SessionStart();

            C2          = new C2ConnectionsModule();
            Hidden      = new HiddenProcessModule();
            NonStandard = new NonStandardModule();
            NetworkScan = new NetworkScanModule();
            Microphone  = new MicrophoneModule();
        }

        public void StartAll()
        {
            SafeStart(C2);
            SafeStart(Hidden);
            SafeStart(NonStandard);
            SafeStart(NetworkScan);
            SafeStart(Microphone);
        }

        public void StopAll()
        {
            SafeStop(C2);
            SafeStop(Hidden);
            SafeStop(NonStandard);
            SafeStop(NetworkScan);
            SafeStop(Microphone);
        }

        public void Stop()
        {
            StopAll();
            EventBus.Stop();
        }

        private static void SafeStart(IDetectionModule m)
        {
            try { m.Start(); }
            catch (Exception ex) { Console.WriteLine("[AntiBot] " + m.Name + " failed to start: " + ex.Message); }
        }

        private static void SafeStop(IDetectionModule m)
        {
            try { m.Stop(); } catch { }
        }
    }
}
