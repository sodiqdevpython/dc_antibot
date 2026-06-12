using System.Collections.Generic;

namespace dc_antibot.AntiBot.Models
{
    public class NetworkImportProfile
    {
        public bool UsesWinsock { get; set; }

        public bool UsesWinInet { get; set; }

        public bool UsesWinHttp { get; set; }

        public bool UsesUrlMon { get; set; }

        public bool UsesDnsApi { get; set; }

        public bool UsesIcmp { get; set; }

        public bool UsesRawSocketApi { get; set; }

        public bool UsesTcpTableEnum { get; set; }

        public Dictionary<string, List<string>> MatchedApis = new Dictionary<string, List<string>>();

        public bool HasAny
        {
            get
            {
                return UsesWinsock || UsesWinInet || UsesWinHttp || UsesUrlMon
                    || UsesDnsApi || UsesIcmp || UsesRawSocketApi || UsesTcpTableEnum;
            }
        }

        public string Summary()
        {
            var parts = new List<string>();
            if (UsesUrlMon)      parts.Add("URLDownloadToFile");
            if (UsesWinInet)     parts.Add("WinINet");
            if (UsesWinHttp)     parts.Add("WinHTTP");
            if (UsesRawSocketApi)parts.Add("RawSocket");
            if (UsesIcmp)        parts.Add("ICMP");
            if (UsesDnsApi)      parts.Add("DNS");
            if (UsesTcpTableEnum)parts.Add("TcpTableEnum");
            if (UsesWinsock)     parts.Add("Winsock");
            return string.Join(",", parts.ToArray());
        }
    }
}
