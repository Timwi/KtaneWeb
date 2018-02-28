using System;
using System.Linq;
using System.Net;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using RT.Servers;
using RT.Util;
using VanillaRuleGenerator;
using HttpStatusCode = RT.Servers.HttpStatusCode;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse vanillaRuleModifier(HttpRequest req)
        {
	        VanillaRuleGenerator.Extensions.Debug.LogMessageHandler = message => _logger?.Log(0, LogType.Debug, message);
	        VanillaRuleGenerator.Extensions.Debug.LogExceptionHandler = (exception, message) =>
	        {
		        _logger?.Log(2, LogType.Error, message);
		        _logger?.Exception(exception);
	        };

			var manualGenerator = new ManualGenerator(_config.VanillaRuleModifierMods, _config.VanillaRuleModifierCache);
	        

            if (!int.TryParse(req.Url["VanillaRuleSeed"], out int seed))
                return manual(req);

            string path = req.Url.Path.Substring(1);

            string modifiedmanual;

            if (path == "")
            {
                modifiedmanual = $"<html><head><title>Repository of Manual pages</title></head><body><h1>Seed = {seed}</h1><ul>";

                modifiedmanual = manualGenerator.GetHTMLFileNames()
                    .Aggregate(modifiedmanual, (current, html) => current + $"<li><a href=\"{WebUtility.UrlEncode(html)}?VanillaRuleSeed={seed}\">{html}</a></li>");

                modifiedmanual += "</ul></body></html>";
            }
            else
            {
                modifiedmanual = manualGenerator.GetHTMLManual(seed, WebUtility.UrlDecode(path), !string.IsNullOrEmpty(_config.VanillaRuleModifierCache));
            }

            IHtmlDocument document = new HtmlParser().Parse(modifiedmanual);

            if (document.Scripts.All(x => x.GetAttribute("src") != "js/highlighter.js"))
            {
                var highlighter = document.CreateElement<IHtmlScriptElement>();
                highlighter.Source = "js/highlighter.js";
                document.Head.Append(highlighter);
            }

            return HttpResponse.Html(document.ToHtml());
        }
    }
}