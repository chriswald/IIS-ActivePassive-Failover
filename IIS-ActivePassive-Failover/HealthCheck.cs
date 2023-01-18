using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace IIS_ActivePassive_Failover
{
    enum HealthCheckMode
    {
        ResponseStatus,
        Match
    }

    internal class HealthCheck
    {
        private readonly IOptionsMonitor<BalancerConfig> _options;
        private readonly ILogger<HealthCheck> _logger;

        private readonly HttpClient _httpClient;
        private string _healthCheckUrl = string.Empty;
        private HealthCheckMode _mode;

        public HealthCheck(IOptionsMonitor<BalancerConfig> options, ILogger<HealthCheck> logger)
        {
            _options = options;
            _logger = logger;
            _httpClient = new HttpClient();

            OnOptionsChanged(_options.CurrentValue);

            _options.OnChange(OnOptionsChanged);
        }

        private void OnOptionsChanged(BalancerConfig config)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(config.HealthCheckTimeout);

            _healthCheckUrl = config.ActiveRootUrl;
            if (!_healthCheckUrl.EndsWith("/")) { _healthCheckUrl += "/"; }
            _healthCheckUrl += config.HealthCheckPath;

            _mode = (config.HealthCheckMode == "Match" ? HealthCheckMode.Match : HealthCheckMode.ResponseStatus);
        }

        public bool Check()
        {
            try
            {
                BalancerConfig config = _options.CurrentValue;
                HttpResponseMessage response;

                if (config.HealthCheckMethod == "GET")
                {
                    response = _httpClient.GetAsync(_healthCheckUrl).Result;
                }
                else if (config.HealthCheckMethod == "POST")
                {
                    response = _httpClient.PostAsync(_healthCheckUrl, null).Result;
                }
                else
                {
                    return false;
                }

                if (_mode == HealthCheckMode.ResponseStatus)
                {
                    return (((int)response.StatusCode).ToString() == config.HealthCheckValue);
                }
                else
                {
                    string body = response.Content.ReadAsStringAsync().Result;
                    return Regex.IsMatch(body, config.HealthCheckValue);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception checking health: {e}");
                return false;
            }
        }
    }
}
