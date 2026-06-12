using System.Net;
using System.Net.Sockets;

namespace dc_antibot.AntiBot.Common
{
    public static class NetworkUtils
    {
        public static bool IsLoopback(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            return ip == "127.0.0.1" || ip == "::1" || ip == "localhost" || ip.StartsWith("127.");
        }

        public static bool IsPrivate(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr)) return false;

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = addr.GetAddressBytes();
                // 10.0.0.0/8
                if (b[0] == 10) return true;
                // 172.16.0.0/12
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
                // 192.168.0.0/16
                if (b[0] == 192 && b[1] == 168) return true;
                // 169.254.0.0/16 link-local
                if (b[0] == 169 && b[1] == 254) return true;
            }
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal) return true;
                byte first = addr.GetAddressBytes()[0];
                if ((first & 0xFE) == 0xFC) return true;
            }

            return false;
        }

        public static bool IsLocalOrPrivate(string ip)
        {
            return IsLoopback(ip) || IsPrivate(ip);
        }

        public static bool IsMulticastOrBroadcast(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr)) return false;

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = addr.GetAddressBytes();
                // 224.0.0.0/4 multicast
                if (b[0] >= 224 && b[0] <= 239) return true;
                // 255.255.255.255 limited broadcast
                if (b[0] == 255 && b[1] == 255 && b[2] == 255 && b[3] == 255) return true;
            }
            else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // ff00::/8 multicast
                byte first = addr.GetAddressBytes()[0];
                if (first == 0xFF) return true;
            }
            return false;
        }

        public static bool IsPublic(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            return !IsLocalOrPrivate(ip) && !IsMulticastOrBroadcast(ip);
        }
    }
}
