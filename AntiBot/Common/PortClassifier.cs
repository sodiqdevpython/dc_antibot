using System.Collections.Generic;

namespace dc_antibot.AntiBot.Common
{
    public static class PortClassifier
    {
        public static readonly Dictionary<int, string> WatchedProtocols = new Dictionary<int, string>
        {
            { 20,  "FTP-data" },
            { 21,  "FTP" },
            { 22,  "SSH/SFTP" },
            { 23,  "Telnet" },
            { 25,  "SMTP" },
            { 110, "POP3" },
            { 143, "IMAP" },
            { 465, "SMTPS" },
            { 587, "SMTP-submission" },
            { 993, "IMAPS" },
            { 995, "POP3S" },
        };

        private static readonly HashSet<int> CommonC2Ports = new HashSet<int>
        {
            4444, 5555, 6666, 8888, 31337, 1337, 9001, 9002
        };

        public static bool IsWatchedProtocol(int port, out string protoName)
        {
            return WatchedProtocols.TryGetValue(port, out protoName);
        }

        public static bool IsCommonC2Port(int port)
        {
            return CommonC2Ports.Contains(port);
        }

        public static bool IsStandardWebPort(int port)
        {
            return port == 80 || port == 443 || port == 8080 || port == 8443;
        }
    }
}
