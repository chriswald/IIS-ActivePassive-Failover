using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IIS_Active_Passive_Failover
{
	enum HealthCheckMode
	{
		ResponseStatus,
		Match
	}

	class HealthCheckConfig
	{
		public string HealthCheckUrl { get; set; }

		public HealthCheckMode Mode { get; set; }

		public string HealthCheckValue { get; set; }

		public int Timeout { get; set; }

		public HealthCheckConfig(string activeRootUrl, string healthCheckPath, string mode, string value, int timeout)
		{

			HealthCheckUrl = activeRootUrl;
			if (!HealthCheckUrl.EndsWith("/")) { HealthCheckUrl += "/"; }
			HealthCheckUrl += healthCheckPath;

			Mode = (mode == "Match" ? HealthCheckMode.Match : HealthCheckMode.ResponseStatus);
			HealthCheckValue = value;
			Timeout = timeout;
		}

		public bool Check()
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HealthCheckUrl);
				request.AutomaticDecompression = DecompressionMethods.GZip;
				request.Timeout = Timeout * 1000;

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					if (Mode == HealthCheckMode.ResponseStatus)
					{
						return (((int)response.StatusCode).ToString() == HealthCheckValue);
					}
					else
					{
						using (Stream stream = response.GetResponseStream())
						using (StreamReader reader = new StreamReader(stream))
						{
							string body = reader.ReadToEnd();
							return Regex.IsMatch(body, HealthCheckValue);
						}
					}
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
