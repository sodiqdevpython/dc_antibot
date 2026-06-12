using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace dc_antibot.AntiBot.Common
{
    public static class WindowInspector
    {
        private const int CacheLifetimeMs = 2000;

        private static readonly object _lock = new object();
        private static DateTime _lastEnumAt = DateTime.MinValue;
        private static HashSet<uint> _pidsWithVisibleWindow = new HashSet<uint>();

        public static bool HasVisibleWindow(int pid)
        {
            if (pid <= 0) return false;
            EnsureFresh();
            lock (_lock)
            {
                return _pidsWithVisibleWindow.Contains((uint)pid);
            }
        }

        private static void EnsureFresh()
        {
            lock (_lock)
            {
                if ((DateTime.Now - _lastEnumAt).TotalMilliseconds < CacheLifetimeMs) return;
                _lastEnumAt = DateTime.Now;
            }

            var fresh = new HashSet<uint>();
            try
            {
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    if (!NativeMethods.IsWindowVisible(hWnd)) return true;

                    int len = NativeMethods.GetWindowTextLength(hWnd);
                    if (len <= 0) return true;

                    uint pid;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out pid);
                    if (pid != 0) fresh.Add(pid);
                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            lock (_lock) { _pidsWithVisibleWindow = fresh; }
        }
    }
}
