using System;
using System.Collections.Generic;

namespace dc_antibot.AntiBot.Common
{
    public static class SuspiciousApiCatalog
    {
        public static readonly HashSet<string> InjectionApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "writeprocessmemory", "createremotethread", "createremotethreadex",
            "virtualallocex", "virtualprotectex",
            "ntmapviewofsection", "ntcreatethreadex", "rtlcreateuserthread",
            "queueuserapc", "ntqueueapcthread", "setthreadcontext",
        };

        public static readonly HashSet<string> AntiDebugApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "isdebuggerpresent", "checkremotedebuggerpresent",
            "ntqueryinformationprocess", "outputdebugstringa", "outputdebugstringw",
        };

        public static readonly HashSet<string> CryptoApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cryptacquirecontexta", "cryptacquirecontextw",
            "cryptencrypt", "cryptgenkey", "cryptderivekey",
            "bcryptencrypt", "bcryptgeneratesymmetrickey",
        };

        public static readonly HashSet<string> AllApis = BuildAll();

        private static HashSet<string> BuildAll()
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in InjectionApis) all.Add(s);
            foreach (var s in AntiDebugApis) all.Add(s);
            foreach (var s in CryptoApis)    all.Add(s);
            return all;
        }
    }

    public class SuspiciousApiProfile
    {
        public int InjectionHits;
        public int AntiDebugHits;
        public int CryptoHits;
        public List<string> Names = new List<string>();

        public bool HasAny { get { return InjectionHits + AntiDebugHits + CryptoHits > 0; } }

        public static SuspiciousApiProfile Build(Dictionary<string, List<string>> iat)
        {
            var p = new SuspiciousApiProfile();
            if (iat == null) return p;
            foreach (var kv in iat)
            {
                foreach (var apiRaw in kv.Value)
                {
                    string api = apiRaw.ToLowerInvariant();
                    if (SuspiciousApiCatalog.InjectionApis.Contains(api)) { p.InjectionHits++; p.Names.Add(apiRaw); }
                    else if (SuspiciousApiCatalog.AntiDebugApis.Contains(api)) { p.AntiDebugHits++; p.Names.Add(apiRaw); }
                    else if (SuspiciousApiCatalog.CryptoApis.Contains(api))    { p.CryptoHits++; p.Names.Add(apiRaw); }
                }
            }
            return p;
        }

        public string Summary()
        {
            var parts = new List<string>();
            if (InjectionHits > 0) parts.Add("Injection×" + InjectionHits);
            if (AntiDebugHits > 0) parts.Add("AntiDebug×" + AntiDebugHits);
            if (CryptoHits    > 0) parts.Add("Crypto×" + CryptoHits);
            return string.Join(",", parts.ToArray());
        }
    }
}
