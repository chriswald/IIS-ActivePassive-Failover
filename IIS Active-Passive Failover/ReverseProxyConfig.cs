using System.Xml;

namespace IIS_Active_Passive_Failover
{
	class ReverseProxyConfig
	{
		public string ActiveRootUrl { get; set; }

		public string PassiveRootUrl { get; set; }

		public string WebConfigPath { get; set; }

		public string ReverseProxyRuleName { get; set; }

		public string InboundSubpath { get; set; }

		private bool isActiveAvailable = false;


		public ReverseProxyConfig(string activeRootUrl, string passiveRootUrl, string webConfigPath, string reverseProxyRuleName, string inboundSubpath)
		{
			ActiveRootUrl = activeRootUrl;
			PassiveRootUrl = passiveRootUrl;
			WebConfigPath = webConfigPath;
			ReverseProxyRuleName = reverseProxyRuleName;
			InboundSubpath = inboundSubpath;

			if (!InboundSubpath.EndsWith("/"))
			{
				InboundSubpath += "/";
			}

			MarkServiceAvailable();
		}

		public void MarkServiceAvailable()
		{
			if (isActiveAvailable) { return; }

			WriteRewriteRule(true);

			isActiveAvailable = true;
		}

		public void MarkServiceDown()
		{
			if (!isActiveAvailable) { return; }

			WriteRewriteRule(false);

			isActiveAvailable = false;
		}

		private void WriteRewriteRule(bool isActive)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(WebConfigPath);

			XmlNode configuration = GetOrCreateChild("configuration", doc, doc);
			XmlNode systemWebServer = GetOrCreateChild("system.webServer", doc, configuration);
			XmlNode rewrite = GetOrCreateChild("rewrite", doc, systemWebServer);
			XmlNode rules = GetOrCreateChild("rules", doc, rewrite);

			XmlNodeList ruleList = ((XmlElement)rules).GetElementsByTagName("rule");

			XmlElement foundRule = null;
			foreach (XmlNode rule in ruleList)
			{
				XmlNode nameAttr = rule.Attributes.GetNamedItem("name");
				if (nameAttr != null && nameAttr.Value == ReverseProxyRuleName)
				{
					foundRule = (XmlElement)rule;
					break;
				}
			}

			if (foundRule == null)
			{
				foundRule = doc.CreateElement("rule");
				foundRule.SetAttribute("name", ReverseProxyRuleName);
				foundRule.SetAttribute("enabled", "true");
				foundRule.SetAttribute("stopProcessing", "true");
				rules.AppendChild(foundRule);
			}

			XmlElement match = (XmlElement)GetOrCreateChild("match", doc, foundRule);
			match.SetAttribute("url", $"{InboundSubpath}(.*)");

			XmlElement action = (XmlElement)GetOrCreateChild("action", doc, foundRule);
			action.SetAttribute("type", "Rewrite");

			string rootUrl = (isActive ? ActiveRootUrl : PassiveRootUrl);
			action.SetAttribute("url", $"{rootUrl}{{R:1}}");

			doc.Save(WebConfigPath);
		}

		private XmlNode GetOrCreateChild(string childName, XmlDocument doc, XmlNode parent)
		{
			XmlNode child = parent.SelectSingleNode(childName);
			if (child == null)
			{
				child = doc.CreateNode(XmlNodeType.Element, childName, doc.NamespaceURI);
				parent.AppendChild(child);
			}

			return child;
		}
	}
}
