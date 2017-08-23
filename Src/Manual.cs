using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.Util.ExtensionMethods;

using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;

namespace KtaneWeb
{
	public sealed partial class KtanePropellerModule
    {
		private HttpResponse manual(HttpRequest req)
		{
			if (req.Url.Path.Length == 0)
				throw new HttpException(HttpStatusCode._404_NotFound);

			string file = req.Url.Path.Substring(1).UrlUnescape();
			string filename = Directory.GetFiles(Path.Combine(_config.Current.BaseDir, "HTML"), file).FirstOrDefault();
			if (filename == null || file.Contains("/") || file.Contains("\\"))
			{
				throw new HttpException(HttpStatusCode._404_NotFound);
			}
			else
			{
				IHtmlDocument document = new HtmlParser().Parse(File.ReadAllText(filename));
				document.Title = Regex.Replace(Path.GetFileNameWithoutExtension(filename), @"\(\w+\)", "").Trim(' ');
				string[] styles = new string[]
				{
					"css/normalize.css",
					"css/main.css",
					"css/font.css",
				};

				var links = document.QuerySelectorAll<IHtmlLinkElement>("link");
				foreach (string style in styles)
				{
					if (links.All(x => x.GetAttribute("href") != style))
					{
						var link = document.CreateElement<IHtmlLinkElement>();
						link.Relation = "stylesheet";
						link.Type = "text/css";
						link.Href = style;
						document.Head.Append(link);
					}
				}

				if (document.Scripts.All(x => x.GetAttribute("src") != "js/highlighter.js"))
				{
					var highlighter = document.CreateElement<IHtmlScriptElement>();
					highlighter.Source ="js/highlighter.js";
					document.Head.Append(highlighter);
				}
				
				return HttpResponse.Html(document.ToHtml());
			}
		}
    }
}
