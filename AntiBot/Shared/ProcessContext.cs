using System;
using System.IO;
using System.Text;
using dc_antibot.AntiBot.Common;
using dc_antibot.AntiBot.Models;
using dc_event_consumer.Ipc;

namespace dc_antibot.AntiBot.Shared
{
    public class ProcessContext
    {
        public int Pid { get; private set; }
        public string Path { get; private set; }
        public string Name { get; private set; }
        public uint SessionId { get; private set; }

        private readonly object _lock = new object();

        private bool _analyzableResolved;  private bool _isAnalyzable;
        private bool _backgroundResolved;  private bool _isBackground;
        private CertSnapshot _cert;
        private bool _iatResolved;         private NetworkImportProfile _netProfile;
        private bool _suspResolved;        private SuspiciousApiProfile _suspProfile;
        private bool _autorunResolved;     private string _autorunLocation;
        private bool _lateAutorunChecked;
        private bool _exclResolved;        private bool _avExcluded; private string _avExclusionMatch;
        private bool _unusualPathResolved; private UnusualPathLevel _unusualPathLevel;

        public readonly DateTime FirstSeen = DateTime.Now;
        public readonly TrafficStats Traffic = new TrafficStats();
        public readonly BeaconTracker Beacon = new BeaconTracker();

        public ProcessContext(int pid, string knownPath, string knownName)
        {
            Pid = pid;
            Path = knownPath;
            Name = knownName;

            if (string.IsNullOrEmpty(Path))
                Path = TryGetPath(pid);

            if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Path))
            {
                try { Name = System.IO.Path.GetFileName(Path); } catch { }
            }

            SessionId = TryGetSessionId(pid);
        }

        public void EnrichPath(string path, string name)
        {
            if (string.IsNullOrEmpty(Path) && !string.IsNullOrEmpty(path))
            {
                Path = path;
                if (string.IsNullOrEmpty(Name))
                    Name = name ?? SafeFileName(path);
            }
        }

        public void NotePid(int pid)
        {
            if (pid <= 0) return;
            lock (_lock)
            {
                if (!_backgroundResolved) Pid = pid;
            }
        }

        public void ApplyCert(CertificateInfo info)
        {
            if (info == null) return;
            var snap = CertSnapshot.From(info);
            if (snap == null) return;

            lock (_lock)
            {
                if (snap.IsBetterThan(_cert)) _cert = snap;
            }
        }

        public CertSnapshot Cert
        {
            get { lock (_lock) { return _cert; } }
        }

        public bool IsAnalyzable
        {
            get
            {
                lock (_lock)
                {
                    if (_analyzableResolved) return _isAnalyzable;
                    _analyzableResolved = true;
                    try { _isAnalyzable = !string.IsNullOrEmpty(Path) && File.Exists(Path); }
                    catch { _isAnalyzable = false; }
                    return _isAnalyzable;
                }
            }
        }

        public bool IsServiceSession
        {
            get { return SessionId == 0; }
        }

        public bool IsBackground
        {
            get
            {
                lock (_lock)
                {
                    if (_backgroundResolved) return _isBackground;
                    _backgroundResolved = true;
                    _isBackground = (SessionId == 0) || !WindowInspector.HasVisibleWindow(Pid);
                    return _isBackground;
                }
            }
        }

        public NetworkImportProfile NetworkImports
        {
            get
            {
                lock (_lock)
                {
                    if (_iatResolved) return _netProfile;
                    _iatResolved = true;
                    var found = IatScanner.ScanFor(Path, NetworkApiCatalog.AllApis);
                    _netProfile = NetworkApiCatalog.BuildProfile(found);
                    return _netProfile;
                }
            }
        }

        public SuspiciousApiProfile SuspiciousApis
        {
            get
            {
                lock (_lock)
                {
                    if (_suspResolved) return _suspProfile;
                    _suspResolved = true;
                    var found = IatScanner.ScanFor(Path, SuspiciousApiCatalog.AllApis);
                    _suspProfile = SuspiciousApiProfile.Build(found);
                    return _suspProfile;
                }
            }
        }

        public UnusualPathLevel UnusualPath
        {
            get
            {
                lock (_lock)
                {
                    if (_unusualPathResolved) return _unusualPathLevel;
                    _unusualPathResolved = true;
                    _unusualPathLevel = PathClassifier.Classify(Path);
                    return _unusualPathLevel;
                }
            }
        }

        public bool IsUnusualPath { get { return UnusualPath != UnusualPathLevel.Normal; } }

        public bool TryRunLateAutorunCheck()
        {
            lock (_lock)
            {
                if (_lateAutorunChecked) return false;
                if ((DateTime.Now - FirstSeen).TotalSeconds < 5) return false;
                if (_autorunResolved && _autorunLocation != null) { _lateAutorunChecked = true; return false; }
                _lateAutorunChecked = true;

                string loc = AutorunChecker.Find(Name, Path);
                if (loc != null)
                {
                    _autorunResolved = true;
                    _autorunLocation = loc;
                    return true;
                }
                return false;
            }
        }

        public string AutorunLocation
        {
            get
            {
                lock (_lock)
                {
                    if (_autorunResolved) return _autorunLocation;
                    _autorunResolved = true;
                    _autorunLocation = AutorunChecker.Find(Name, Path);
                    return _autorunLocation;
                }
            }
        }

        public bool IsAutorun { get { return AutorunLocation != null; } }

        public bool IsInAvExclusion
        {
            get
            {
                lock (_lock)
                {
                    if (_exclResolved) return _avExcluded;
                    _exclResolved = true;
                    string m;
                    _avExcluded = ExclusionChecker.IsExcluded(Path, out m);
                    _avExclusionMatch = m;
                    return _avExcluded;
                }
            }
        }

        public string AvExclusionMatch { get { var _ = IsInAvExclusion; return _avExclusionMatch; } }

        public bool ShouldSkip
        {
            get { return TrustEvaluator.ShouldSkip(Cert); }
        }

        public float ScoreMultiplier
        {
            get { return TrustEvaluator.ScoreMultiplier(Cert); }
        }

        public string SignatureSummary()
        {
            var c = Cert;
            if (c == null) return IsAnalyzable ? "?" : "unreadable";
            return c.SummaryTag();
        }

        private static string TryGetPath(int pid)
        {
            IntPtr h = IntPtr.Zero;
            try
            {
                h = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (h == IntPtr.Zero) return null;
                var sb = new StringBuilder(1024);
                uint cap = (uint)sb.Capacity;
                if (NativeMethods.QueryFullProcessImageName(h, 0, sb, ref cap))
                    return sb.ToString();
            }
            catch { }
            finally { if (h != IntPtr.Zero) NativeMethods.CloseHandle(h); }
            return null;
        }

        private static uint TryGetSessionId(int pid)
        {
            uint sid;
            if (NativeMethods.ProcessIdToSessionId((uint)pid, out sid)) return sid;
            return 0;
        }

        private static string SafeFileName(string p)
        {
            if (string.IsNullOrEmpty(p)) return null;
            try { return System.IO.Path.GetFileName(p); } catch { return null; }
        }
    }
}
