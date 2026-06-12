using System;
using dc_antibot.AntiBot.Models;

namespace dc_antibot.AntiBot.Core
{
    public interface IDetectionModule
    {
        string Name { get; }
        bool IsRunning { get; }

        void Start();
        void Stop();

        event EventHandler<DetectionResult> OnDetection;
    }
}
