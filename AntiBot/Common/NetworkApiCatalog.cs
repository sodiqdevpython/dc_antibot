using System;
using System.Collections.Generic;
using dc_antibot.AntiBot.Models;

namespace dc_antibot.AntiBot.Common
{
    public static class NetworkApiCatalog
    {
        public static readonly HashSet<string> AllApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "socket", "connect", "send", "recv", "sendto", "recvfrom",
            "wsastartup", "wsaconnect", "wsasend", "wsarecv",
            "gethostbyname", "getaddrinfo",
            "wsasocketa", "wsasocketw",

            "internetopena", "internetopenw",
            "internetconnecta", "internetconnectw",
            "internetopenurla", "internetopenurlw",
            "httpopenrequesta", "httpopenrequestw",
            "httpsendrequesta", "httpsendrequestw",
            "internetreadfile",

            "winhttpopen", "winhttpconnect", "winhttpopenrequest",
            "winhttpsendrequest", "winhttpreceiveresponse",

            "urldownloadtofilea", "urldownloadtofilew", "urldownloadtocachefilea",

            "dnsquery_a", "dnsquery_w", "dnsqueryex",

            "icmpsendecho", "icmpsendecho2", "icmpcreatefile",
            "getextendedtcptable", "getextendedudptable",
        };

        public static NetworkImportProfile BuildProfile(Dictionary<string, List<string>> found)
        {
            var p = new NetworkImportProfile();
            if (found == null || found.Count == 0) return p;
            p.MatchedApis = found;

            foreach (var kv in found)
            {
                foreach (var apiRaw in kv.Value)
                {
                    string api = apiRaw.ToLowerInvariant();

                    switch (api)
                    {
                        case "urldownloadtofilea":
                        case "urldownloadtofilew":
                        case "urldownloadtocachefilea":
                            p.UsesUrlMon = true;
                            break;

                        case "wsasocketa":
                        case "wsasocketw":
                            p.UsesRawSocketApi = true;
                            p.UsesWinsock = true;
                            break;

                        case "icmpsendecho":
                        case "icmpsendecho2":
                        case "icmpcreatefile":
                            p.UsesIcmp = true;
                            break;

                        case "getextendedtcptable":
                        case "getextendedudptable":
                            p.UsesTcpTableEnum = true;
                            break;

                        case "dnsquery_a":
                        case "dnsquery_w":
                        case "dnsqueryex":
                            p.UsesDnsApi = true;
                            break;

                        default:
                            if (api.StartsWith("internet") || api.StartsWith("http"))
                                p.UsesWinInet = true;
                            else if (api.StartsWith("winhttp"))
                                p.UsesWinHttp = true;
                            else
                                p.UsesWinsock = true;
                            break;
                    }
                }
            }

            return p;
        }
    }
}
