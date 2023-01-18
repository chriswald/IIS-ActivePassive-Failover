using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IIS_ActivePassive_Failover
{
    internal class ReverseProxy
    {
        private readonly IOptionsMonitor<BalancerConfig> _options;
        private readonly ILogger<ReverseProxy> _logger;

        private bool _isActiveAvailable = false;

        public ReverseProxy(IOptionsMonitor<BalancerConfig> options, ILogger<ReverseProxy> logger)
        {
            _options = options;
            _logger = logger;

            MarkServiceAvailable();
        }

        public void MarkServiceAvailable()
        {
            if (_isActiveAvailable) { return; }

            _logger.LogInformation("Marking service available");

            WriteRewriteRule(true);

            _isActiveAvailable = true;
        }

        public void MarkServiceDown()
        {
            if (!_isActiveAvailable) { return; }

            _logger.LogInformation("Marking service down");

            WriteRewriteRule(false);

            _isActiveAvailable = false;
        }

        private void WriteRewriteRule(bool isActive)
        {
            BalancerConfig config = _options.CurrentValue;

            XmlDocument doc = new XmlDocument();
            doc.Load(config.WebConfigPath);

            XmlNode configuration = GetOrCreateChild("configuration", doc, doc);
            XmlNode systemWebServer = GetOrCreateChild("system.webServer", doc, configuration);
            XmlNode rewrite = GetOrCreateChild("rewrite", doc, systemWebServer);
            XmlNode rules = GetOrCreateChild("rules", doc, rewrite);

            XmlNodeList ruleList = ((XmlElement)rules).GetElementsByTagName("rule");

            XmlElement? foundRule = null;
            foreach (XmlNode rule in ruleList)
            {
                XmlAttributeCollection? attributes = rule.Attributes;
                if (attributes == null)
                {
                    continue;
                }

                XmlNode? nameAttr = attributes.GetNamedItem("name");
                if (nameAttr != null && nameAttr.Value == config.ReverseProxyRuleName)
                {
                    foundRule = (XmlElement)rule;
                    break;
                }
            }

            if (foundRule == null)
            {
                foundRule = doc.CreateElement("rule");
                foundRule.SetAttribute("name", config.ReverseProxyRuleName);
                foundRule.SetAttribute("enabled", "true");
                foundRule.SetAttribute("stopProcessing", "true");
                rules.AppendChild(foundRule);
            }

            XmlElement match = (XmlElement)GetOrCreateChild("match", doc, foundRule);
            match.SetAttribute("url", $"{EndWithSlash(config.InboundSubpath)}(.*)");

            XmlElement action = (XmlElement)GetOrCreateChild("action", doc, foundRule);
            action.SetAttribute("type", "Rewrite");

            string rootUrl = (isActive ? EndWithSlash(config.ActiveRootUrl) : EndWithSlash(config.PassiveRootUrl));
            action.SetAttribute("url", $"{rootUrl}{{R:1}}");

            doc.Save(config.WebConfigPath);
        }

        private XmlNode GetOrCreateChild(string childName, XmlDocument doc, XmlNode parent)
        {
            XmlNode? child = parent.SelectSingleNode(childName);
            if (child == null)
            {
                child = doc.CreateNode(XmlNodeType.Element, childName, doc.NamespaceURI);
                parent.AppendChild(child);
            }

            return child;
        }

        private string EndWithSlash(string path)
        {
            if (!path.EndsWith("/"))
            {
                return path + "/";
            }
            else
            {
                return path;
            }
        }
    }
}
