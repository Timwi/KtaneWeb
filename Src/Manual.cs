using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse manual(HttpRequest req)
        {
            if (int.TryParse(req.Url["VanillaRuleSeed"], out int _) || int.TryParse(req.Url["RuleSeed"], out int _))
                return vanillaRuleModifier(req);

            var htmlDir = Path.Combine(_config.BaseDir, "HTML");

            try
            {
                var file = req.Url.Path.Substring(1).UrlUnescape();

                var fileInfo = new DirectoryInfo(htmlDir).EnumerateFiles(file, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (file.Contains("/") || file.Contains("\\") || fileInfo == null || fileInfo.Name != file)
                    return new FileSystemHandler(htmlDir).Handle(req);

                IHtmlDocument document = new HtmlParser().Parse(File.ReadAllText(fileInfo.FullName));
                document.Title = Regex.Replace(Path.GetFileNameWithoutExtension(fileInfo.Name), @"\(\w+\)", "").Trim(' ');
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
                    highlighter.Source = "js/highlighter.js";
                    document.Head.Append(highlighter);
                }

                return HttpResponse.Html(document.ToHtml());
            }
            catch
            {
                return new FileSystemHandler(htmlDir).Handle(req);
            }
        }
    }
}
