using IIS_Active_Passive_Failover;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Harness
{
	class Program
	{
		static void Main(string[] args)
		{
			ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
			configMap.ExeConfigFilename = @"IIS Active-Passive Failover.exe.config";
			Service1 service = new Service1();
			service.ExternalRun(configMap);
		}
	}
}
