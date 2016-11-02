using System.IO;
using System.Linq;
using RT.PropellerApi;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule : PropellerModuleBase<KtaneWebConfig>
    {
        public override string Name => "Keep Talking and Nobody Explodes — Mods and Modules";

        private static string TypeToString(KtaneModuleType type)
        {
            switch (type)
            {
                case KtaneModuleType.Regular:
                    return "Regular";
                case KtaneModuleType.Needy:
                    return "Needy";
            }
            return null;
        }

        private SelectableData[] _datas;

        public override HttpResponse Handle(HttpRequest req)
        {
            _datas = (_datas ?? Ut.NewArray(
                new SelectableData { HumanReadable = "HTML manual", DataAttributeName = "html", DataAttributeValue = mod => File.Exists(Path.Combine(Settings.HtmlDir, mod.Name + ".html")) ? $"{Settings.HtmlUrl}/{mod.Name}.html" : null },
                new SelectableData { HumanReadable = "PDF manual", DataAttributeName = "pdf", DataAttributeValue = mod => File.Exists(Path.Combine(Settings.PdfDir, mod.Name + ".pdf")) ? $"{Settings.PdfUrl}/{mod.Name}.pdf" : null },
                new SelectableData { HumanReadable = "Steam Workshop Item", DataAttributeName = "steam", DataAttributeValue = mod => mod.SteamUrl },
                new SelectableData { HumanReadable = "Source code", DataAttributeName = "source", DataAttributeValue = mod => mod.SourceUrl }));

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Keep Talking and Nobody Explodes — Mods and Modules"),
                    new LINK { href = "//fonts.googleapis.com/css?family=Special+Elite", rel = "stylesheet", type = "text/css" },
                    new STYLELiteral(Css),
                    new SCRIPT { src = "https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js" },
                    new SCRIPTLiteral(JavaScript)),
                new BODY()._(
                    new DIV { class_ = "heading" }._(
                        new IMG { class_ = "logo", src = Settings.LogoUrl },
                        new DIV { class_ = "filters" }._(
                            new DIV { class_ = "head" }._("Filters:"),
                            new DIV { class_ = "filter-section" }._(
                                new DIV { class_ = "sub" }._("Types:"),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-regular" }, " ", new LABEL { for_ = "filter-regular" }._("Regular")),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-needy" }, " ", new LABEL { for_ = "filter-needy" }._("Needy"))),
                            new DIV { class_ = "filter-section" }._(
                                new DIV { class_ = "sub" }._("Origin:"),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-vanilla" }, " ", new LABEL { for_ = "filter-vanilla" }._("Vanilla")),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-mods" }, " ", new LABEL { for_ = "filter-mods" }._("Mods")))),
                        new DIV { class_ = "selectables" }._(
                            new DIV { class_ = "head" }._("Make links go to:"),
                            _datas.Select(d => new DIV(new LABEL { id = $"selectable-label-{d.DataAttributeName}", for_ = $"selectable-{d.DataAttributeName}" }._(d.HumanReadable), " ", new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{d.DataAttributeName}" }.Data("selectable", d.DataAttributeName))))),
                    new DIV { class_ = "subheading" },
                    new TABLE(
                        new TR(
                            new TH { colspan = 2 }._("Manual"),
                            new TH("Name"),
                            new TH("Type"),
                            new TH("Author(s)"),
                            new TH("Steam ID"),
                            new TH("Source")),
                        Settings.KtaneModules.Where(mod => mod.Name != null).OrderBy(mod => mod.Name).Select(mod => new TR { class_ = "mod" }.Data("type", mod.Type.ToString()).Data("origin", mod.Origin.ToString())._(
                            new TD { class_ = "manual-icon" }._(Settings.HtmlDir == null || !File.Exists(Path.Combine(Settings.HtmlDir, mod.Name + ".html")) ? null : new A { href = $"{Settings.HtmlUrl}/{mod.Name}.html" }._(new IMG { class_ = "icon", src = Settings.HtmlIconUrl })),
                            new TD { class_ = "manual-icon" }._(Settings.PdfDir == null || !File.Exists(Path.Combine(Settings.PdfDir, mod.Name + ".pdf")) ? null : new A { href = $"{Settings.PdfUrl}/{mod.Name}.pdf" }._(new IMG { class_ = "icon", src = Settings.PdfIconUrl })),
                            new TD(_datas.Aggregate((Tag) new A { class_ = "modlink", href = $"{Settings.PdfDir}/{mod.Name}.pdf" }, (p, n) => p.Data(n.DataAttributeName, n.DataAttributeValue(mod)))._(mod.Name)),
                            new TD(TypeToString(mod.Type)),
                            new TD(mod.Author),
                            new TD(mod.SteamID?.Apply(steamID => new A { href = $"http://steamcommunity.com/sharedfiles/filedetails/?id={steamID}" }._(steamID))),
                            new TD(mod.SourceUrl?.Apply(source => new A { href = mod.SourceUrl }._("Source")))))))));
        }
    }
}
