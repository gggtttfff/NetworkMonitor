using System;

namespace NetworkMonitor
{
    public class NetworkCheckOptions
    {
        public string LoginUrl { get; set; } = "http://2.2.2.2";
        public string PrimaryDns { get; set; } = "www.baidu.com";
        public string SecondaryDns { get; set; } = "baidu.com";
        public int PingTimeout { get; set; } = 10000;
    }

    public class NetworkCheckResult
    {
        public bool IsConnected { get; set; }
        public bool GatewayUnreachable { get; set; }
    }
}
