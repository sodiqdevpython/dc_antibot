namespace dc_antibot.AntiBot.Core
{
    public class ModuleConfig
    {
        public bool EnableC2Connections { get; set; }
        public bool EnableHiddenProcessConnections { get; set; }
        public bool EnableNonStandardConnection { get; set; }
        public bool EnableNetworkScanning { get; set; }
        public bool EnableScreenCapture { get; set; }
        public bool EnableKeylogger { get; set; }
        public bool EnableMicrophone { get; set; }

        public static ModuleConfig AllEnabled()
        {
            return new ModuleConfig
            {
                EnableC2Connections = true,
                EnableHiddenProcessConnections = true,
                EnableNonStandardConnection = true,
                EnableNetworkScanning = true,
                EnableScreenCapture = true,
                EnableKeylogger = true,
                EnableMicrophone = true,
            };
        }
    }
}
