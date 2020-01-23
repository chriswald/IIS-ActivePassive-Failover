using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

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

		public string Method { get; set; }

		public HealthCheckMode Mode { get; set; }

		public string HealthCheckValue { get; set; }

		private HttpClient httpClient;

		public HealthCheckConfig(string activeRootUrl, string method, string healthCheckPath, string mode, string value, int timeout)
		{
			HealthCheckUrl = activeRootUrl;
			if (!HealthCheckUrl.EndsWith("/")) { HealthCheckUrl += "/"; }
			HealthCheckUrl += healthCheckPath;

			Method = method;
			Mode = (mode == "Match" ? HealthCheckMode.Match : HealthCheckMode.ResponseStatus);
			HealthCheckValue = value;

			httpClient = new HttpClient();
			httpClient.Timeout = new System.TimeSpan(0, 0, timeout);
		}

		public bool Check()
		{
			try
			{
				HttpResponseMessage response = null;

				if (Method == "GET")
				{
					response = httpClient.GetAsync(HealthCheckUrl).Result;
				}
				else if (Method == "POST")
				{
					response = httpClient.PostAsync(HealthCheckUrl, null).Result;
				}
				else
				{
					return false;
				}

				if (Mode == HealthCheckMode.ResponseStatus)
				{
					return (((int)response.StatusCode).ToString() == HealthCheckValue);
				}
				else
				{
					string body = response.Content.ReadAsStringAsync().Result;
					return Regex.IsMatch(body, HealthCheckValue);
				}
			}
			catch (Exception e)
			{
				EventLog.WriteEntry("IIS Active-Passive Failover", $"Exception checking health: {e.ToString()}", EventLogEntryType.Information);
				return false;
			}
		}
	}
}
