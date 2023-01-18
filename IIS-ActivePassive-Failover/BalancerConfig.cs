namespace IIS_ActivePassive_Failover
{
    internal class BalancerConfig
    {
        public string ActiveRootUrl { get; set; } = string.Empty;

        public string PassiveRootUrl { get; set; } = string.Empty;

        public string WebConfigPath { get; set; } = string.Empty;

        public string ReverseProxyRuleName { get; set; } = string.Empty;

        public string InboundSubpath { get; set; } = string.Empty;

        public string HealthCheckPath { get; set; } = string.Empty;

        public string HealthCheckMethod { get; set; } = string.Empty;

        public string HealthCheckMode { get; set; } = string.Empty;

        public string HealthCheckValue { get; set; } = string.Empty;

        public int HealthCheckTimeout { get; set; } = 0;

        public int HealthCheckInterval { get; set; } = 0;

        public int SlowStart { get; set; } = 0;

        public int SlowStop { get; set; } = 0;
    }
}
