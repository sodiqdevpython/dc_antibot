using System;
using dc_antibot.AntiBot.Core;
using dc_antibot.AntiBot.Models;

namespace dc_antibot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var antibot = new AntiBotManager(ModuleConfig.AllEnabled());

            //antibot.C2.OnScoreUp            += OnC2ScoreUp;
            //antibot.C2.OnDetection += OnC2Detection;
            //antibot.Hidden.OnDetection += OnHiddenDetection;
            //antibot.NonStandard.OnDetection += OnNonStandardDetection;
            //antibot.NetworkScan.OnDetection += OnNetworkScanDetection;
            antibot.Microphone.OnDetection    += OnMicrophoneDetection;

            antibot.Start();

            //foreach (var name in antibot.ModuleNames)
            //    Console.WriteLine(name + ": " + (antibot.IsModuleRunning(name) ? "ON" : "OFF"));

            Console.ReadLine();
            antibot.Stop();
        }

        static void OnC2ScoreUp(object sender, ScoreUpEventArgs e)
        {
            Console.WriteLine(
                "[C2 SCORE+] PID:" + e.Pid + " " + (e.ProcessName ?? "?") +
                " +" + e.Delta.ToString("0.0") + " => " + e.Score + "/10 | " + e.Reason);
        }

        static void OnC2Detection(object sender, DetectionResult r)
        {
            Console.WriteLine(
                "[C2 ALERT] PID:" + r.Pid + " " + (r.ProcessName ?? "?") +
                " " + r.Score + "/10 | " + r.Reason);
        }

        static void OnHiddenDetection(object sender, DetectionResult r)
        {
            Console.WriteLine(
                "[HIDDEN ALERT] PID:" + r.Pid + " " + (r.ProcessName ?? "?") +
                " " + r.Score + "/10 | " + r.Reason);
        }

        static void OnNonStandardDetection(object sender, DetectionResult r)
        {
            Console.WriteLine(
                "[NONSTANDARD ALERT] PID:" + r.Pid + " " + (r.ProcessName ?? "?") +
                " " + r.Score + "/10 | " + r.Reason);
        }

        static void OnNetworkScanDetection(object sender, DetectionResult r)
        {
            Console.WriteLine(
                "[SCAN ALERT] PID:" + r.Pid + " " + (r.ProcessName ?? "?") +
                " " + r.Score + "/10 | " + r.Reason);
        }

        static void OnMicrophoneDetection(object sender, DetectionResult r)
        {
            Console.WriteLine(
                "[MICROPHONE ALERT] PID:" + r.Pid + " " + (r.ProcessName ?? "?") +
                " | " + r.Reason);
        }
    }
}
