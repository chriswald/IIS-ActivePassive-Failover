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
		private ReverseProxyConfig ReverseProxyConfig { get; set; }

		private CancellationTokenSource CancellationTokenSource { get; set; }

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

			ReverseProxyConfig = new ReverseProxyConfig(activeUrl, passiveUrl, webConfigPath, ruleName, inboundSubpath);

			Run();
		}

		private void Run()
		{
			CancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = CancellationTokenSource.Token;

			Task.Run(() =>
			{
				while (true)
				{
					if (token.IsCancellationRequested)
					{
						break;
					}


				}
			}, token);
		}

		protected override void OnStop()
		{
			CancellationTokenSource.Cancel();
		}
	}
}
