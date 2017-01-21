using System.IO;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse mainPage(HttpRequest req, KtaneWebConfig config)
        {
            // Access keys:
            // A
            // B
            // C
            // D
            // E    
            // F
            // G
            // H
            // I
            // J    JSON
            // K
            // L
            // M    Manual
            // N    Needy
            // O    Mods
            // P
            // Q
            // R    Regular
            // S    Show missing
            // T    Tutorial video
            // U    Source code
            // V    Vanilla
            // W    Steam Workshop Item
            // X
            // Y
            // Z

            var sheets = config.KtaneModules.ToDictionary(mod => mod.Name, mod => config.EnumerateSheetUrls(mod.Name, config.KtaneModules.Select(m => m.Name).Where(m => m != mod.Name && m.StartsWith(mod.Name)).ToArray()));

            var selectables = Ut.NewArray(
                new Selectable
                {
                    HumanReadable = "Manual",
                    Accel = 'M',
                    Icon = mod => new IMG { class_ = "icon manual-icon", title = "Manual", alt = "Manual", src = sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["icon"].GetString() : null },
                    DataAttributeName = "manual",
                    DataAttributeValue = mod => sheets.Get(mod.Name, null)?.ToString(),
                    Url = mod => sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["url"].GetString() : null,
                    ShowIcon = mod => sheets[mod.Name].Count > 0,
                    CssClass = "manual"
                },
                new Selectable
                {
                    HumanReadable = "Steam Workshop Item",
                    Accel = 'W',
                    Icon = mod => new IMG { class_ = "icon", title = "Steam Workshop Item", alt = "Steam Workshop Item", src = config.SteamIconUrl },
                    DataAttributeName = "steam",
                    DataAttributeValue = mod => mod.SteamID?.Apply(s => $"http://steamcommunity.com/sharedfiles/filedetails/?id={s}"),
                    Url = mod => $"http://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamID}",
                    ShowIcon = mod => mod.SteamID != null
                },
                new Selectable
                {
                    HumanReadable = "Source code",
                    Accel = 'u',
                    Icon = mod => new IMG { class_ = "icon", title = "Source code", alt = "Source code", src = config.UnityIconUrl },
                    DataAttributeName = "source",
                    DataAttributeValue = mod => mod.SourceUrl,
                    Url = mod => mod.SourceUrl,
                    ShowIcon = mod => mod.SourceUrl != null
                },
                new Selectable
                {
                    HumanReadable = "Tutorial video",
                    Accel = 'T',
                    Icon = mod => new IMG { class_ = "icon", title = "Tutorial video", alt = "Tutorial video", src = config.TutorialVideoIconUrl },
                    DataAttributeName = "video",
                    DataAttributeValue = mod => mod.TutorialVideoUrl,
                    Url = mod => mod.TutorialVideoUrl,
                    ShowIcon = mod => mod.TutorialVideoUrl != null
                });

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages"),
                    new LINK { href = "//fonts.googleapis.com/css?family=Special+Elite", rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js" },
                    new SCRIPT { src = req.Url.WithParent("js").ToHref() },
                    new META { name = "viewport", content = "width=device-width" }),
                new BODY(
                    new DIV { class_ = "heading" }._(
                        new IMG { class_ = "logo", src = config.LogoUrl },
                        new DIV { class_ = "filters" }._(
                            new DIV { class_ = "head" }._("Filters:"),
                            new DIV { class_ = "filter-section" }._(
                                new DIV { class_ = "sub" }._("Types:"),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-regular" }, " ", new LABEL { for_ = "filter-regular", accesskey = "r" }._("Regular".Accel('R'))),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-needy" }, " ", new LABEL { for_ = "filter-needy", accesskey = "n" }._("Needy".Accel('N')))),
                            new DIV { class_ = "filter-section" }._(
                                new DIV { class_ = "sub" }._("Origin:"),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-vanilla" }, " ", new LABEL { for_ = "filter-vanilla", accesskey = "v" }._("Vanilla".Accel('V'))),
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-mods" }, " ", new LABEL { for_ = "filter-mods", accesskey = "o" }._("Mods".Accel('o'))))),
                        new DIV { class_ = "selectables" }._(
                            new DIV { class_ = "head" }._("Make links go to:"),
                            selectables.Select(sel => new DIV(
                                new LABEL { id = $"selectable-label-{sel.DataAttributeName}", for_ = $"selectable-{sel.DataAttributeName}", accesskey = sel.Accel.ToString().ToLowerInvariant() }._(sel.HumanReadable.Accel(sel.Accel)), " ",
                                new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{sel.DataAttributeName}" }.Data("selectable", sel.DataAttributeName))),
                            new DIV(
                                new LABEL { for_ = "filter-nonexist", accesskey = "s" }._("Show missing".Accel('S')), " ",
                                new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-nonexist" }))),
                    new DIV { class_ = "subheading" },
                    new TABLE(
                        new TR(
                            new TH("Links"),
                            new TH("Name"),
                            new TH("Type"),
                            new TH("Author(s)")),
                        config.KtaneModules.Select(mod => selectables.Aggregate(new TR { class_ = "mod" }.Data("type", mod.Type.ToString()).Data("origin", mod.Origin.ToString()).Data("mod", mod.Name), (p, n) => p.Data(n.DataAttributeName, n.DataAttributeValue(mod)))._(
                            new TD { class_ = "icons" }._(selectables.Select(sel => sel.ShowIcon(mod) ? new A { href = sel.Url(mod), class_ = sel.CssClass }._(sel.Icon(mod)) : null)),
                            new TD(new A { class_ = "modlink" }._(mod.Icon(config), mod.Name)),
                            new TD(mod.Type.ToString()),
                            new TD(mod.Author)))),
                    new DIV { class_ = "links" }._(new A { href = "/json", accesskey = "j" }._("See JSON".Accel('J'))),
                    new DIV { class_ = "credits" }._("Icons by lumbud84 and samfun123."),
                    new DIV { class_ = "extra-links" }._(
                        new P("Additional resources:"),
                        new UL(
                            new LI(new A { href = "https://www.dropbox.com/s/paluom4wlogjdl0/ModsOnlyManual_Sorted_A-Z.pdf?dl=0" }._("Rexkix’s Sorted A–Z manual (mods only)")),
                            new LI(new A { href = "https://www.dropbox.com/s/4bkfwoa4d7p0a7z/ModsOnlyManual_Sorted_A-Z_with_Cheat_Sheets.pdf?dl=0" }._("Rexkix’s Sorted A–Z manual with cheat sheets (mods only)")),
                            new LI(new A { href = "https://www.dropbox.com/s/hp3a3vgpbhsrbbs/CheatSheet.pdf?dl=0" }._("Elias’s extremely condensed manual (mods ", new EM("and"), " vanilla)")),
                            new LI(new A { href = "Highlighter/Highlighter.html" }._("Samfun123’s table highlighter")))))));
        }
    }
}
