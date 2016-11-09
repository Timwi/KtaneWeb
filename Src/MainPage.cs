using System.IO;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        public HttpResponse MainPage(HttpRequest req, KtaneWebConfig config)
        {
            // Access keys:
            // A
            // B
            // C    Source code
            // D
            // E    PDF manual embellished
            // F
            // G
            // H    HTML manual
            // I
            // J    JSON
            // K
            // L
            // M    Mods
            // N    Needy
            // O
            // P    PDF manual
            // Q
            // R    Regular
            // S    Show missing
            // T
            // U
            // V    Vanilla
            // W    Steam Workshop Item
            // X
            // Y
            // Z

            var selectables = Ut.NewArray(
                new Selectable
                {
                    HumanReadable = "PDF manual",
                    Accel = 'P',
                    IconUrl = config.PdfIconUrl,
                    DataAttributeName = "pdf",
                    Url = mod => File.Exists(Path.Combine(config.PdfDir, mod.Name + ".pdf")) ? $"{config.PdfUrl}/{mod.Name}.pdf" : null,
                    AltUrl = mod => null
                },
                new Selectable
                {
                    HumanReadable = "PDF manual embellished",
                    Accel = 'e',
                    IconUrl = config.PdfEmbellishedIconUrl,
                    DataAttributeName = "pdfe",
                    Url = mod => File.Exists(Path.Combine(config.PdfDir, mod.Name + " (embellished).pdf")) ? $"{config.PdfUrl}/{mod.Name} (embellished).pdf" : null,
                    AltUrl = mod => File.Exists(Path.Combine(config.PdfDir, mod.Name + ".pdf")) ? $"{config.PdfUrl}/{mod.Name}.pdf" : null
                },
                new Selectable
                {
                    HumanReadable = "HTML manual",
                    Accel = 'H',
                    IconUrl = config.HtmlIconUrl,
                    DataAttributeName = "html",
                    Url = mod => File.Exists(Path.Combine(config.HtmlDir, mod.Name + ".html")) ? $"{config.HtmlUrl}/{mod.Name}.html" : null,
                    AltUrl = mod => null
                },
                new Selectable
                {
                    HumanReadable = "Steam Workshop Item",
                    Accel = 'W',
                    IconUrl = config.SteamIconUrl,
                    DataAttributeName = "steam",
                    Url = mod => mod.SteamUrl,
                    AltUrl = mod => null
                },
                new Selectable
                {
                    HumanReadable = "Source code",
                    Accel = 'c',
                    IconUrl = config.UnityIconUrl,
                    DataAttributeName = "source",
                    Url = mod => mod.SourceUrl,
                    AltUrl = mod => null
                });

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages"),
                    new LINK { href = "//fonts.googleapis.com/css?family=Special+Elite", rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js" },
                    new SCRIPTLiteral(JavaScript),
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
                                new DIV(new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-mods" }, " ", new LABEL { for_ = "filter-mods", accesskey = "m" }._("Mods".Accel('M'))))),
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
                        config.KtaneModules.Select(mod => new TR { class_ = "mod" }.Data("type", mod.Type.ToString()).Data("origin", mod.Origin.ToString())._(
                            new TD { class_ = "icons" }._(selectables.Select(sel => sel.Url(mod) == null ? null : new A { href = sel.Url(mod) }._(new IMG { class_ = "icon", title = sel.HumanReadable, alt = sel.HumanReadable, src = sel.IconUrl }))),
                            new TD(selectables.Aggregate((Tag) new A { class_ = "modlink", href = $"{config.PdfDir}/{mod.Name}.pdf" }, (p, n) => p.Data(n.DataAttributeName, n.Url(mod) ?? n.AltUrl(mod)))._(mod.Icon(config), mod.Name)),
                            new TD(mod.Type.ToString()),
                            new TD(mod.Author)))),
                    new DIV { class_ = "json-link" }._(new A { href = "/json", accesskey = "j" }._("See JSON".Accel('J'))),
                    new DIV { class_ = "credits" }._("Icons by lumbud84. Embellished PDFs by ",
                        new SPAN { class_ = "credited", title = "Mouse In The Maze, Orientation Cube, Perspective Pegs, Piano Keys, Silly Slots, Two Bits" }._("Rexkix"), ", ",
                        new SPAN { class_ = "credited", title = "Cryptography, Microcontrollers, Resistors, Semaphore, Switches" }._("Nanthelas")," and ",
                        new SPAN { class_ = "credited", title = "3D Maze, Laundry, Murder" }._("Timwi"), "."),
                    new DIV { class_ = "extra-links" }._(
                        new P("Additional resources:"),
                        new UL(
                            new LI(new A { href = "https://www.dropbox.com/s/paluom4wlogjdl0/ModsOnlyManual_Sorted_A-Z.pdf?dl=0" }._("Rexkix’s Sorted A–Z manual (mods only)")),
                            new LI(new A { href = "https://www.dropbox.com/s/4bkfwoa4d7p0a7z/ModsOnlyManual_Sorted_A-Z_with_Cheat_Sheets.pdf?dl=0" }._("Rexkix’s Sorted A–Z manual with cheat sheets (mods only)")),
                            new LI(new A { href = "https://www.dropbox.com/s/hp3a3vgpbhsrbbs/CheatSheet.pdf?dl=0" }._("Elias’s extremely condensed manual (mods ", new EM("and"), " vanilla)")))))));
        }
    }
}
