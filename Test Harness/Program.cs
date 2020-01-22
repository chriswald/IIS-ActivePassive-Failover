using IIS_Active_Passive_Failover;
using System;
using System.Configuration;

namespace Test_Harness
{
	class Program
	{
		static void Main(string[] args)
		{
			ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
			configMap.ExeConfigFilename = @"IISFailover.exe.config";
			Service1 service = new Service1();

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
			{
				Console.WriteLine("Canceling...");
				eventArgs.Cancel = true;
				service.ExternalStop();
			};

			service.ExternalRun(configMap);
		}
	}
}
