using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using Microsoft.Win32;

namespace dc_antibot.AntiBot.Common
{
    public static class ExclusionChecker
    {
        private const int RefreshSeconds = 60;
        private static readonly object _lock = new object();

        private static DateTime _loadedAt = DateTime.MinValue;
        private static HashSet<string> _pathExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> _processExclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _lastSource = "";

        public static bool IsExcluded(string filePath, out string matched)
        {
            matched = null;
            if (string.IsNullOrEmpty(filePath)) return false;

            EnsureLoaded();

            string target = Normalize(filePath);
            string fileName = SafeFileName(filePath);

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(fileName) && _processExclusions.Contains(fileName))
                {
                    matched = fileName + " (process name, " + _lastSource + ")";
                    return true;
                }

                foreach (var raw in _pathExclusions)
                {
                    string excl = Normalize(raw);
                    if (excl.Length == 0) continue;

                    if (target.Equals(excl, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = raw + " (exact file, " + _lastSource + ")";
                        return true;
                    }
                    if (target.StartsWith(excl + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        matched = raw + " (folder, " + _lastSource + ")";
                        return true;
                    }
                }
            }
            return false;
        }

        private static void EnsureLoaded()
        {
            lock (_lock)
            {
                if ((DateTime.Now - _loadedAt).TotalSeconds < RefreshSeconds && _loadedAt != DateTime.MinValue)
                    return;

                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var procs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sources = new List<string>();

                if (TryLoadFromWmi(paths, procs)) sources.Add("WMI");
                if (TryLoadFromRegistry(paths))   sources.Add("registry");

                _pathExclusions = paths;
                _processExclusions = procs;
                _lastSource = sources.Count == 0 ? "none" : string.Join("+", sources.ToArray());
                _loadedAt = DateTime.Now;
            }
        }

        private static bool TryLoadFromWmi(HashSet<string> paths, HashSet<string> procs)
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                scope.Connect();
                using (var searcher = new ManagementObjectSearcher(scope,
                    new ObjectQuery("SELECT ExclusionPath, ExclusionProcess FROM MSFT_MpPreference")))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        try
                        {
                            var pArr = obj["ExclusionPath"] as string[];
                            if (pArr != null)
                                foreach (var v in pArr) if (!string.IsNullOrEmpty(v)) paths.Add(v);

                            var procArr = obj["ExclusionProcess"] as string[];
                            if (procArr != null)
                                foreach (var v in procArr) if (!string.IsNullOrEmpty(v)) procs.Add(v);
                        }
                        catch { }
                        finally { obj.Dispose(); }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static readonly string[] ExclusionKeys =
        {
            @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths",
            @"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Paths",
        };

        private static bool TryLoadFromRegistry(HashSet<string> paths)
        {
            bool any = false;
            foreach (var keyPath in ExclusionKeys)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (key == null) continue;
                        foreach (var raw in key.GetValueNames())
                            if (!string.IsNullOrEmpty(raw)) paths.Add(raw);
                        any = true;
                    }
                }
                catch { }
            }
            return any;
        }

        private static string Normalize(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            return p.Trim().TrimEnd('\\', '/').ToLowerInvariant();
        }

        private static string SafeFileName(string p)
        {
            if (string.IsNullOrEmpty(p)) return null;
            try { return Path.GetFileName(p).ToLowerInvariant(); } catch { return null; }
        }
    }
}
