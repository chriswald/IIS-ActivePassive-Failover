using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IIS_ActivePassive_Failover
{
    internal class Service : BackgroundService
    {
        private readonly IHost _host;
        private readonly IOptionsMonitor<BalancerConfig> _options;
        private readonly HealthCheck _healthCheck;
        private readonly ReverseProxy _reverseProxy;

        private int _numFailAfterSuccess = 0;
        private int _numSuccessAfterFail = 0;
        private int _slowStartCount = 0;
        private int _slowStopCount = 0;

        public Service(IHost host, IOptionsMonitor<BalancerConfig> options, HealthCheck healthCheck, ReverseProxy reverseProxy) 
        {
            _host = host;
            _options = options;
            _healthCheck = healthCheck;
            _reverseProxy = reverseProxy;

            OnOptionsChanged(_options.CurrentValue);

            _options.OnChange(OnOptionsChanged);
        }

        private void OnOptionsChanged(BalancerConfig config)
        {
            _slowStartCount = config.SlowStart;
            _slowStopCount = config.SlowStop;
            _numSuccessAfterFail = _slowStartCount; // Assume everything's good to start
            _numFailAfterSuccess = 0; // Assume everything's good to start
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_healthCheck.Check())
                    {
                        _numFailAfterSuccess = 0;

                        if (_numSuccessAfterFail < (_slowStartCount - 1))
                        {
                            _numSuccessAfterFail++;
                        }
                        else
                        {
                            _reverseProxy.MarkServiceAvailable();
                        }
                    }
                    else
                    {
                        _numSuccessAfterFail = 0;

                        if (_numFailAfterSuccess < (_slowStopCount - 1))
                        {
                            _numFailAfterSuccess++;
                        }
                        else
                        {
                            _reverseProxy.MarkServiceDown();
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_options.CurrentValue.HealthCheckInterval));
                }

                await _host.StopAsync();
            });
        }
    }
}
