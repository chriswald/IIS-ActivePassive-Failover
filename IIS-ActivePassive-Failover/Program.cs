using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.Runtime.InteropServices;

namespace IIS_ActivePassive_Failover
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((HostBuilderContext hostBuilderContext, IServiceCollection services) =>
                {
                    IConfiguration config = hostBuilderContext.Configuration;

                    // Disable the service shutdown timeout
                    services.Configure<HostOptions>(opts => opts.ShutdownTimeout = Timeout.InfiniteTimeSpan);

                    services.AddSingleton<ReverseProxy>();
                    services.AddSingleton<HealthCheck>();
                    services.AddHostedService<Service>();
                })
                .ConfigureAppConfiguration((IConfigurationBuilder configBuilder) =>
                {
                    configBuilder.AddXmlFile("appsettings.xml", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((HostBuilderContext hostContext, ILoggingBuilder logging) =>
                {
                    logging.ClearProviders();

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        logging.AddEventLog((EventLogSettings settings) =>
                        {
                            settings.SourceName = "IIS Active-Passive Failover";
                            settings.LogName = "Application";
                        });

#if DEBUG
                        logging.AddConsole();
#endif
                    }
                    else
                    {
                        logging.AddConsole();
                    }
                });
        }
    }
}