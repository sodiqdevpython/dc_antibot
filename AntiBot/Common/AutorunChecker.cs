using System;
using System.IO;
using Microsoft.Win32;

namespace dc_antibot.AntiBot.Common
{
    public static class AutorunChecker
    {
        public static string Find(string processName, string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;

            try
            {
                string r;
                if ((r = CheckRun(Registry.CurrentUser,  @"Software\Microsoft\Windows\CurrentVersion\Run", processName, fullPath)) != null) return "HKCU\\...\\Run (" + r + ")";
                if ((r = CheckRun(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", processName, fullPath)) != null) return "HKLM\\...\\Run (" + r + ")";
                if ((r = CheckRun(Registry.CurrentUser,  @"Software\Microsoft\Windows\CurrentVersion\RunOnce", processName, fullPath)) != null) return "HKCU\\...\\RunOnce";
                if ((r = CheckRun(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", processName, fullPath)) != null) return "HKLM\\...\\RunOnce";
                if ((r = CheckRun(Registry.CurrentUser,  @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run", processName, fullPath)) != null) return "HKCU\\...\\Policies\\Explorer\\Run";
                if ((r = CheckRun(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run", processName, fullPath)) != null) return "HKLM\\...\\Policies\\Explorer\\Run";


                if (CheckStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), processName)) return "Startup folder (user)";
                if (CheckStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), processName)) return "Startup folder (common)";
            }
            catch { }

            return null;
        }

        private static string CheckRun(RegistryKey root, string keyPath, string name, string fullPath)
        {
            try
            {
                using (var key = root.OpenSubKey(keyPath))
                {
                    if (key == null) return null;
                    string nameLow = (name ?? "").ToLowerInvariant();
                    string pathLow = fullPath.ToLowerInvariant();
                    foreach (var valueName in key.GetValueNames())
                    {
                        string value = (key.GetValue(valueName) ?? "").ToString().ToLowerInvariant();
                        if (value.Length == 0) continue;
                        if ((nameLow.Length > 0 && value.Contains(nameLow)) || value.Contains(pathLow))
                            return valueName;
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool CheckStartupFolder(string folderPath, string name)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return false;
                string baseName = Path.GetFileNameWithoutExtension(name ?? "");

                foreach (var file in Directory.GetFiles(folderPath, "*.lnk"))
                    if (Path.GetFileNameWithoutExtension(file).Equals(baseName, StringComparison.OrdinalIgnoreCase))
                        return true;

                foreach (var file in Directory.GetFiles(folderPath, "*.exe"))
                    if (Path.GetFileName(file).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            catch { }
            return false;
        }
    }
}
