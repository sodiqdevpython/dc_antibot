using System;
using dc_antibot.AntiBot.Models;

namespace dc_antibot.AntiBot.Core
{
    public abstract class ModuleBase : IDetectionModule
    {
        public abstract string Name { get; }
        public bool IsRunning { get; private set; }

        public event EventHandler<DetectionResult> OnDetection;

        public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            try { OnStart(); }
            catch { IsRunning = false; throw; }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            try { OnStop(); }
            finally { IsRunning = false; }
        }

        protected abstract void OnStart();
        protected abstract void OnStop();

        protected void Emit(DetectionResult result)
        {
            if (result == null) return;
            if (result.ModuleName == null) result.ModuleName = Name;
            result.Severity = SeverityHelper.FromScore(result.Score);

            var h = OnDetection;
            if (h == null) return;

            foreach (EventHandler<DetectionResult> single in h.GetInvocationList())
            {
                try { single(this, result); }
                catch {  }
            }
        }
    }
}
