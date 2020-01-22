using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIS_Active_Passive_Failover
{
	public partial class Service1 : ServiceBase
	{
		private volatile CancellationTokenSource CancellationTokenSource = null;

		private Thread Worker = null;

		private ExeConfigurationFileMap FileMap { get; set; }

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
				configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			}


			string activeUrl = configuration.AppSettings.Settings["ActiveRootUrl"].Value;
			string passiveUrl = configuration.AppSettings.Settings["PassiveRootUrl"].Value;
			string webConfigPath = configuration.AppSettings.Settings["WebConfigPath"].Value;
			string ruleName = configuration.AppSettings.Settings["ReverseProxyRuleName"].Value;
			string inboundSubpath = configuration.AppSettings.Settings["InboundSubpath"].Value;

			ReverseProxyConfig reverseProxyConfig = new ReverseProxyConfig(activeUrl, passiveUrl, webConfigPath, ruleName, inboundSubpath);

			string healthCheckPath = configuration.AppSettings.Settings["HealthCheckPath"].Value;
			string mode = configuration.AppSettings.Settings["HealthCheckMode"].Value;
			string healthCheckValue = configuration.AppSettings.Settings["HealthCheckValue"].Value;
			string timeoutString = configuration.AppSettings.Settings["HealthCheckTimeout"].Value;
			string intervalString = configuration.AppSettings.Settings["HealthCheckInterval"].Value;

			int timeout = int.Parse(timeoutString);
			int interval = int.Parse(intervalString);

			HealthCheckConfig healthCheckConfig = new HealthCheckConfig(activeUrl, healthCheckPath, mode, healthCheckValue, timeout);

			CancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = CancellationTokenSource.Token;

			Worker = new Thread(() => { Run(token, reverseProxyConfig, healthCheckConfig, interval); });
			Worker.Start();
		}

		private void Run(CancellationToken token, ReverseProxyConfig reverseProxyConfig, HealthCheckConfig healthCheckConfig, int interval)
		{
			while (!CancellationTokenSource.IsCancellationRequested)
			{
				if (healthCheckConfig.Check())
				{
					reverseProxyConfig.MarkServiceAvailable();
				}
				else
				{
					reverseProxyConfig.MarkServiceDown();
				}

				Thread.Sleep(interval * 1000);
			}
		}

		protected override void OnStop()
		{
			CancellationTokenSource?.Cancel();
			Worker?.Join();
		}
	}
}
