using System.Configuration;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace IIS_Active_Passive_Failover
{
	public partial class Service1 : ServiceBase
	{
		private volatile CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

		private ExeConfigurationFileMap FileMap { get; set; }

		private volatile int slowStartCount;

		private volatile int numSuccessAfterFail;

		public Service1()
		{
			InitializeComponent();
		}

		public void ExternalRun(ExeConfigurationFileMap map)
		{
			FileMap = map;
			OnStart(null);
		}

		public void ExternalStop()
		{
			this.OnStop();
		}

		protected override void OnStart(string[] args)
		{
			Configuration configuration;
			if (FileMap != null)
			{
				configuration = ConfigurationManager.OpenMappedExeConfiguration(FileMap, ConfigurationUserLevel.None);
			}
			else
			{
				Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
				configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			}


			string activeUrl = configuration.AppSettings.Settings["ActiveRootUrl"].Value;
			string passiveUrl = configuration.AppSettings.Settings["PassiveRootUrl"].Value;
			string webConfigPath = configuration.AppSettings.Settings["WebConfigPath"].Value;
			string ruleName = configuration.AppSettings.Settings["ReverseProxyRuleName"].Value;
			string inboundSubpath = configuration.AppSettings.Settings["InboundSubpath"].Value;

			ReverseProxyConfig reverseProxyConfig = new ReverseProxyConfig(activeUrl, passiveUrl, webConfigPath, ruleName, inboundSubpath);

			string healthCheckPath = configuration.AppSettings.Settings["HealthCheckPath"].Value;
			string method = configuration.AppSettings.Settings["HealthCheckMethod"].Value;
			string mode = configuration.AppSettings.Settings["HealthCheckMode"].Value;
			string healthCheckValue = configuration.AppSettings.Settings["HealthCheckValue"].Value;
			string timeoutString = configuration.AppSettings.Settings["HealthCheckTimeout"].Value;
			string intervalString = configuration.AppSettings.Settings["HealthCheckInterval"].Value;

			int timeout = int.Parse(timeoutString);
			int interval = int.Parse(intervalString);

			HealthCheckConfig healthCheckConfig = new HealthCheckConfig(activeUrl, method, healthCheckPath, mode, healthCheckValue, timeout);

			string slowStart = configuration.AppSettings.Settings["SlowStart"].Value;
			slowStartCount = int.Parse(slowStart);
			numSuccessAfterFail = slowStartCount; // Assume everything's good to start

			Thread thread = new Thread(() => { Run(reverseProxyConfig, healthCheckConfig, interval); });
			thread.Start();
		}

		private void Run(ReverseProxyConfig reverseProxyConfig, HealthCheckConfig healthCheckConfig, int interval)
		{
			while (!CancellationTokenSource.IsCancellationRequested)
			{
				if (healthCheckConfig.Check())
				{
					if (numSuccessAfterFail < (slowStartCount - 1))
					{
						numSuccessAfterFail++;
					}
					else
					{
						reverseProxyConfig.MarkServiceAvailable();
					}
				}
				else
				{
					numSuccessAfterFail = 0;
					reverseProxyConfig.MarkServiceDown();
				}

				Thread.Sleep(interval * 1000);
			}
		}

		protected override void OnStop()
		{
			CancellationTokenSource.Cancel();
		}
	}
}
