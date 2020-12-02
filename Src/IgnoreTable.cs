using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse ignoreTable(HttpRequest req)
        {
            var moduleInfos = getModuleInfoCache();
            var ignoringModules = moduleInfos.Modules.Where(m => m.Ignore != null && m.Ignore.Length > 0).OrderBy(m => m.SortKey).ToArray();
            var ignoredModules = moduleInfos.Modules.Where(m => ignoringModules.Contains(m) || m.IsSemiBoss || m.IsFullBoss).OrderBy(m => m.SortKey).ToArray();

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Ignore list table"),
                    new LINK { href = "../HTML/css/font.css", rel = "stylesheet", type = "text/css" },
                    new STYLELiteral(@"
                        body { font-family: 'Trebuchet MS'; }
                        table { border-collapse: collapse; margin: 0 auto; }
                        td { border: 1px solid #acf; padding: 0; }
                        td.corner { border: none; }
                        td.ignored { background: #cdf; }
                        img { display: block; }
                        h1 { text-align: center; border-bottom: 1px solid #888; }
                    ")),
                new BODY(
                    new H1("Table of modules that ignore one another"),
                    new TABLE(
                        // Header row
                        new TR(
                            new TD { class_ = "corner" },
                            ignoredModules.Select(im => new TH { class_ = "ignored-module" }._(new IMG { src = $"../Icons/{im.FileName ?? im.Name}.png", title = im.Name }))),
                        // Rest of the table
                        ignoringModules.Select(ignoring => new TR(
                            new TH { class_ = "ignoring-module" }._(new IMG { src = $"../Icons/{ignoring.FileName ?? ignoring.Name}.png", title = ignoring.Name }),
                            ignoredModules.Select(im => 
                            ((ignoring.Ignore.Contains(im.DisplayName ?? im.Name) || (ignoring.Ignore.Contains("+FullBoss") && im.IsFullBoss) || (ignoring.Ignore.Contains("+SemiBoss") && im.IsSemiBoss)) && 
                            !ignoring.Ignore.Contains("-" + (im.DisplayName ?? im.Name))).Apply(ig => new TD { class_ = ig ? "ignored" : null, title = ig ? $"{ignoring.Name} ignores {im.Name}" : null }))))))));
        }
    }
}
