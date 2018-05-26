using System;
using System.IO;
using System.IO.Compression;
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
        private HttpResponse mainPage(HttpRequest req)
        {
            // Access keys:
            // A    Logfile Analyzer
            // B
            // C    link to Source code
            // D    sort by date published
            // E    sort by expert difficulty
            // F    Find
            // G
            // H
            // I    include missing
            // J    JSON
            // K    Dark Theme
            // L    Light Theme
            // M    include/exclude mods
            // N    sort by name
            // O    sort by defuser difficulty
            // P    Profile Editor
            // Q
            // R    include/exclude regular modules
            // S    link to Steam Workshop item
            // T    link to Tutorial video
            // U    link to Manual
            // V    include/exclude vanilla
            // W    include/exclude widgets
            // X
            // Y    include/exclude needy modules
            // Z
            // .    More

            var sheets = _config.Current.KtaneModules.ToDictionary(mod => mod.Name, mod => _config.EnumerateSheetUrls(mod.Name, _config.Current.KtaneModules.Select(m => m.Name).Where(m => m != mod.Name && m.StartsWith(mod.Name)).ToArray()));

            var selectables = Ut.NewArray(
                new Selectable
                {
                    HumanReadable = "Manual",
                    Accel = 'u',
                    Icon = mod => new IMG { class_ = "icon manual-icon", title = "Manual", alt = "Manual", src = sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["icon"].GetString() : null },
                    DataAttributeName = "manual",
                    DataAttributeValue = mod => sheets.Get(mod.Name, null)?.ToString(),
                    Url = mod => sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["url"].GetString() : null,
                    ShowIcon = mod => sheets[mod.Name].Count > 0,
                    CssClass = "manual"
                },
                new Selectable
                {
                    HumanReadable = "Steam Workshop",
                    Accel = 'S',
                    Icon = mod => new IMG { class_ = "icon", title = "Steam Workshop", alt = "Steam Workshop", src = "HTML/img/steam-workshop-item.png" },
                    DataAttributeName = "steam",
                    DataAttributeValue = mod => mod.SteamID?.Apply(s => $"http://steamcommunity.com/sharedfiles/filedetails/?id={s}"),
                    Url = mod => $"http://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamID}",
                    ShowIcon = mod => mod.SteamID != null
                },
                new Selectable
                {
                    HumanReadable = "Source code",
                    Accel = 'c',
                    Icon = mod => new IMG { class_ = "icon", title = "Source code", alt = "Source code", src = "HTML/img/unity.png" },
                    DataAttributeName = "source",
                    DataAttributeValue = mod => mod.SourceUrl,
                    Url = mod => mod.SourceUrl,
                    ShowIcon = mod => mod.SourceUrl != null
                },
                new Selectable
                {
                    HumanReadable = "Tutorial video",
                    Accel = 'T',
                    Icon = mod => new IMG { class_ = "icon", title = "Tutorial video", alt = "Tutorial video", src = "HTML/img/video.png" },
                    DataAttributeName = "video",
                    DataAttributeValue = mod => mod.TutorialVideoUrl,
                    Url = mod => mod.TutorialVideoUrl,
                    ShowIcon = mod => mod.TutorialVideoUrl != null
                });

            var filters = Ut.NewArray(
                KtaneFilter.Checkboxes("origin", "Origin", mod => mod.Origin),
                KtaneFilter.Checkboxes("type", "Type", mod => mod.Type),
                KtaneFilter.Checkboxes("twitchplays", "Twitch Plays", mod => mod.TwitchPlaysSupport),
                KtaneFilter.Slider("defdiff", "Defuser difficulty", mod => mod.DefuserDifficulty),
                KtaneFilter.Slider("expdiff", "Expert difficulty", mod => mod.ExpertDifficulty));

            var displays = Ut.NewArray(
                new { Readable = "Description", Id = "description" },
                //new { Readable = "Author", Id = "author" },
                //new { Readable = "Type", Id = "type" },
                new { Readable = "Difficulty", Id = "difficulty" },
                new { Readable = "Origin", Id = "origin" },
                new { Readable = "Twitch support", Id = "twitch" },
                new { Readable = "Date published", Id = "published" },
                new { Readable = "Module ID", Id = "id" });

            var cssLink = req.Url.WithParent("css");
#if DEBUG
            cssLink = cssLink.WithQuery("u", DateTime.UtcNow.Ticks.ToString());
#endif

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages"),
                    new LINK { href = req.Url.WithParent("HTML/css/font.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = cssLink.ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("HTML/css/dark-theme.css").ToHref(), id = "theme-css", rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "HTML/js/jquery.3.1.1.min.js" },
                    new SCRIPT { src = "HTML/js/jquery-ui.1.12.1.min.js" },
                    new LINK { href = req.Url.WithParent("HTML/css/jquery-ui.1.12.1.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPTLiteral($@"
                        Ktane = {{
                            Filters: {filters.Select(f => f.ToJson()).ToJsonList()},
                            Selectables: {selectables.Select(s => s.DataAttributeName).ToJsonList()},
                            Themes: {{
                                'dark': 'HTML/css/dark-theme.css'
                            }}
                        }};
                    "),
                    new SCRIPT { src = req.Url.WithParent("js").ToHref() },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" }),
                new BODY(
                    new DIV { id = "main-content" }._(
                        new DIV { id = "logo" }._(new IMG { src = "HTML/img/repo-logo.png" }),
                        new DIV { id = "icons", class_ = "icons" }._(
                            new DIV { class_ = "icon-page shown" }._(
                                new DIV { class_ = "icon" }._(new A { href = "More/On%20the%20Subject%20of%20Entering%20the%20World%20of%20Mods.html" }._(new IMG { class_ = "icon", src = "HTML/img/google-docs.png" }, new SPAN("Intro to Playing with Mods"))),
                                new DIV { class_ = "icon" }._(new A { href = "More/Logfile%20Analyzer.html", accesskey = "a" }._(new IMG { class_ = "icon", src = "HTML/img/logfile-analyzer.png" }, new SPAN("Logfile Analyzer".Accel('A')))),
                                new DIV { class_ = "icon" }._(new A { href = "More/Profile%20Editor.html", id = "profiles-link" }._(new IMG { class_ = "icon", src = "HTML/img/profile-editor.png" }, new SPAN { id = "profiles-rel" }._("Profiles"))),
                                new DIV { class_ = "icon" }._(new A { href = "https://discord.gg/Fv7YEDj" }._(new IMG { class_ = "icon", src = "HTML/img/discord.png" }, new SPAN("Join us on Discord")))),
                            new DIV { class_ = "icon-page" }._(
                                new DIV { class_ = "icon" }._(new A { href = "https://www.youtube.com/playlist?list=PL23fILnY52_2-I6JNG_7jw69x5YXj11GN" }._(new IMG { class_ = "icon", src = "HTML/img/video-playlist.png" }, new SPAN("Tutorial videos playlist"))),
                                new DIV { class_ = "icon" }._(new A { href = "https://docs.google.com/document/d/1fFkBprpo1CMy-EJ-TyD6C_NoX1_7kgiOFeCRdBsh6hk/edit?usp=sharing" }._(new IMG { class_ = "icon", src = "HTML/img/google-docs.png" }, new SPAN("Intro to Making Mods")))
                                //new DIV { class_ = "icon" }._(new A { href = "More/Translating%20FAQ.html", id = "translating-faq" }._(new IMG { class_ = "icon", src = "HTML/img/translate.png" }, new SPAN("Help us translate manuals")))
                                ),
                            new DIV { class_ = "icon" }._(new A { href = "#", id = "icon-page-next" }._(new IMG { class_ = "icon", src = "HTML/img/more.png" }, new SPAN("More")))),

                        new A { href = "#", class_ = "mobile-opt", id = "page-opt" },

                        new DIV { id = "top-controls" }._(
                            new DIV { class_ = "search-container" }._(
                                new LABEL { for_ = "search-field" }._("Find: ".Accel('F')),
                                new INPUT { id = "search-field", accesskey = "f" }, " ",
                                new SCRIPTLiteral("document.getElementById('search-field').focus();"),
                                new A { href = "#", id = "search-field-clear" },
                                new DIV { class_ = "search-options" }._(
                                    new SPAN { class_ = "search-option", id = "search-opt-names" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-names" }, new LABEL { for_ = "search-names" }._("Names")),
                                    new SPAN { class_ = "search-option", id = "search-opt-authors" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-authors" }, new LABEL { for_ = "search-authors" }._("Authors")),
                                    new SPAN { class_ = "search-option", id = "search-opt-descriptions" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-descriptions" }, new LABEL { for_ = "search-descriptions" }._("Descriptions"))))),

                        new DIV { id = "main-table-container" }._(
                            new DIV { id = "more-tab" }._(new A { href = "#", id = "more-link", accesskey = "." }._("Filters & more")),

                            new TABLE { id = "main-table" }._(
                                new TR { class_ = "header-row" }._(
                                    new TH { colspan = selectables.Length }._("Links"),
                                    new TH { class_ = "modlink" }._(new A { href = "#", class_ = "sort-header" }._("Name")),
                                    new TH { class_ = "infos" }._(new A { href = "#", class_ = "sort-header" }._("Information"))),
                                _config.Current.KtaneModules.Select(mod =>
                                    new TR { class_ = "mod" }
                                        .Data("mod", mod.Name)
                                        .Data("author", mod.Author)
                                        .Data("description", mod.Description)
                                        .Data("sortkey", mod.SortKey)
                                        .Data("published", mod.Published.ToString("yyyy-MM-dd"))
                                        .Data("compatibility", mod.Compatibility.ToString())
                                        .AddData(selectables, sel => sel.DataAttributeName, sel => sel.DataAttributeValue(mod))
                                        .AddData(filters, flt => flt.DataAttributeName, flt => flt.GetDataAttributeValue(mod))
                                        ._(
                                            selectables.Select((sel, ix) => new TD { class_ = "selectable" + (ix == selectables.Length - 1 ? " last" : null) + sel.CssClass?.Apply(c => " " + c) }._(sel.ShowIcon(mod) ? new A { href = sel.Url(mod), class_ = sel.CssClass }._(sel.Icon(mod)) : null)),
                                            new TD { class_ = "infos-1" }._(new DIV { class_ = "modlink-wrap" }._(new A { class_ = "modlink" }._(mod.Icon(_config), new SPAN { class_ = "mod-name" }._(mod.Name)))),
                                            new TD { class_ = "infos-2" }._(new DIV { class_ = "infos" }._(
                                                new DIV { class_ = "inf-type" }._(mod.Type.ToString()),
                                                new DIV { class_ = "inf-origin" }._(mod.Origin.ToString()),
                                                mod.Type != KtaneModuleType.Regular && mod.Type != KtaneModuleType.Needy ? null : mod.DefuserDifficulty == mod.ExpertDifficulty
                                                    ? new DIV { class_ = "inf-difficulty" }._(new SPAN { class_ = "inf-difficulty-sub" }._(mod.DefuserDifficulty.Value.ToReadable()))
                                                    : new DIV { class_ = "inf-difficulty" }._(new SPAN { class_ = "inf-difficulty-sub" }._(mod.DefuserDifficulty.Value.ToReadable()), " (d), ", new SPAN { class_ = "inf-difficulty-sub" }._(mod.ExpertDifficulty.Value.ToReadable()), " (e)"),
                                                new DIV { class_ = "inf-author" }._(mod.Author),
                                                new DIV { class_ = "inf-published" }._(mod.Published.ToString("yyyy-MMM-dd")),
                                                new DIV { class_ = "inf-twitch" },
                                                mod.ModuleID.NullOr(id => new DIV { class_ = "inf-id" }._(id)),
                                                new DIV { class_ = "inf-description" }._(mod.Description))),
                                            new TD { class_ = "mobile-ui" }._(new A { href = "#", class_ = "mobile-opt" }))))),

                        new DIV { id = "module-count" },

                        new DIV { id = "profiles-menu", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new P { class_ = "editor" }._(new A { href = "More/Profile%20Editor.html", accesskey = "p" }._("Open Profile Editor".Accel('P'))),
                            new P { class_ = "heading" }._("Download profiles by difficulty:"),
                            new P { class_ = "zip" }._(new A { href = "/profile/zip" }._("Download all as ZIP")),
                            new DIV { class_ = "wrapper" }._(
                                new DIV { class_ = "defuser" }._(
                                    new P("By defuser difficulty:"),
                                    new MENU(EnumStrong.GetValues<KtaneModuleDifficulty>().Select(d => new LI(new A { href = "/profile/defuser/" + d }._(d.ToReadable())))),
                                    new P { class_ = "explain" }._("These are veto profiles, i.e. you can use these to ", new EM("disable"), " certain modules.")),
                                new DIV { class_ = "expert" }._(
                                    new P("By expert difficulty:"),
                                    new MENU(EnumStrong.GetValues<KtaneModuleDifficulty>().Select(d => new LI(new A { href = "/profile/expert/" + d }._(d.ToReadable())))),
                                    new P { class_ = "explain" }._("These are expert profiles, i.e. you can use these to ", new EM("include"), " certain modules.")))),

                        new DIV { id = "more", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "filters" }._(
                                new DIV { class_ = "display" }._(
                                    new H4("Display:"),
                                    displays.Select(dspl => new DIV(
                                        new INPUT { id = "display-" + dspl.Id, name = "display", value = dspl.Id, class_ = "display", type = itype.checkbox },
                                        new LABEL { for_ = "display-" + dspl.Id }._("\u00a0", dspl.Readable)))),
                                new DIV { class_ = "site-theme" }._(
                                    new H4("Site theme:"),
                                    new DIV(
                                        new INPUT { type = itype.radio, class_ = "set-theme", name = "theme", id = "theme-default" }.Data("theme", "null"), " ",
                                        new LABEL { for_ = "theme-default", accesskey = "l" }._("Light".Accel('L'))),
                                    new DIV(
                                        new INPUT { type = itype.radio, class_ = "set-theme", name = "theme", id = "theme-dark" }.Data("theme", "dark"), " ",
                                        new LABEL { for_ = "theme-dark", accesskey = "k" }._("Dark".Accel('k')))),
                                filters.Select(filter => new DIV { class_ = "filter " + filter.DataAttributeName }._(filter.ToHtml())),
                                new DIV { class_ = "sort" }._(
                                    new H4("Sort order:"),
                                    new DIV(
                                        new INPUT { id = "sort-name", name = "sort", value = "name", class_ = "sort", type = itype.radio },
                                        new LABEL { for_ = "sort-name", accesskey = "n" }._("\u00a0Sort by name".Accel('n'))),
                                    new DIV(
                                        new INPUT { id = "sort-defuser-difficulty", name = "sort", value = "defdiff", class_ = "sort", type = itype.radio },
                                        new LABEL { for_ = "sort-defuser-difficulty", accesskey = "o" }._("\u00a0Sort by defuser difficulty".Accel('o'))),
                                    new DIV(
                                        new INPUT { id = "sort-expert-difficulty", name = "sort", value = "expdiff", class_ = "sort", type = itype.radio },
                                        new LABEL { for_ = "sort-expert-difficulty", accesskey = "e" }._("\u00a0Sort by expert difficulty".Accel('e'))),
                                    new DIV(
                                        new INPUT { id = "sort-published", name = "sort", value = "published", class_ = "sort", type = itype.radio },
                                        new LABEL { for_ = "sort-published", accesskey = "d" }._("\u00a0Sort by date published".Accel('d')))),
                                new DIV { class_ = "link-targets" }._(
                                    new H4("Make links go to:"),
                                    selectables.Select(sel => new DIV(
                                        new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{sel.DataAttributeName}" }.Data("selectable", sel.DataAttributeName), " ",
                                        new LABEL { class_ = "set-selectable", id = $"selectable-label-{sel.DataAttributeName}", for_ = $"selectable-{sel.DataAttributeName}", accesskey = sel.Accel?.ToString().ToLowerInvariant() }._(sel.HumanReadable.Accel(sel.Accel)))),
                                    new DIV { id = "include-missing" }._(
                                        new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-include-missing" }, " ",
                                        new LABEL { for_ = "filter-include-missing", accesskey = "i" }._("Include missing".Accel('I'))))),

                            new UL { class_ = "dev" }._(
                                new LI(new A { href = "More/Experting Template.png" }._("Experting template")),
                                new LI(new A { href = "https://form.jotform.com/62686042776162" }._("Submit an idea for a new mod")),
                                new LI(new A { href = "https://form.jotform.com/62718595122156" }._("Find a mod idea to implement"))),
                            new DIV { class_ = "highlighting-controls" }._(
                                new H3("Controls to highlight elements in HTML manuals"),
                                new TABLE { class_ = "highlighting-controls" }._(
                                    new TR(new TH("Control"), new TH("Function")),
                                    new TR(new TD("Ctrl+Click (Windows)", new BR(), "Command+Click (Mac)"), new TD("Highlight a table column")),
                                    new TR(new TD("Shift+Click"), new TD("Highlight a table row")),
                                    new TR(new TD("Alt+Click (Windows)", new BR(), "Ctrl+Shift+Click (Windows)", new BR(), "Command+Shift+Click (Mac)"), new TD("Highlight a table cell or an item in a list")),
                                    new TR(new TD("Alt+1, Alt+2, Alt+3, Alt+4", new TD("Change highlighter color"))))),
                            new H3("Default file locations"),
                            new H4("Windows"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH("Game:"), new TD(new CODE(@"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes"))),
                                new TR(new TH("Logfile (Steam):"), new TD(new CODE(@"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt"))),
                                new TR(new TH("Logfile (Oculus):"), new TD(new CODE(@"C:\Program Files (x86)\Oculus\Software\steel-crate-games-keep-talking-and-nobody-explodes\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt"))),
                                new TR(new TH("Mod Selector Profiles:"), new TD(new CODE(@"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\ModProfiles"))),
                                new TR(new TH("Mod Settings:"), new TD(new CODE(@"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\Modsettings"))),
                                new TR(new TH("Screenshots (Steam):"), new TD(new CODE(@"C:\Program Files (x86)\Steam\userdata\<some number>\760\remote\341800\screenshots")))),
                            new H4("Mac"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH("Game:"), new TD(new CODE(@"~/Library/Application Support/Steam/steamapps/common/Keep Talking and Nobody Explodes"))),
                                new TR(new TH("Logfile:"), new TD(new CODE(@"~/Library/Logs/Unity/Player.log"))),
                                new TR(new TH("Mod Selector Profiles:"), new TD(new CODE(@"~/Library/Application Support/com.steelcrategames.keeptalkingandnobodyexplodes/ModProfiles"))),
                                new TR(new TH("Mod Settings:"), new TD(new CODE(@"~/Library/Application Support/com.steelcrategames.keeptalkingandnobodyexplodes/Modsettings"))),
                                new TR(new TH("Screenshots (Steam):"), new TD(new CODE(@"~/Library/Application Support/Steam/userdata/<some number>/760/remote/341800/screenshots")))),
                            new H4("Linux"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH("Game:"), new TD(new CODE(@"~/.steam/steamapps/common/Keep Talking and Nobody Explodes"))),
                                new TR(new TH("Logfile:"), new TD(new CODE(@"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/Player.log"))),
                                new TR(new TH("Mod Selector Profiles:"), new TD(new CODE(@"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/ModProfiles"))),
                                new TR(new TH("Mod Settings:"), new TD(new CODE(@"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/Modsettings"))),
                                new TR(new TH("Screenshots (Steam):"), new TD(new CODE(@"~/.steam/userdata/<some number>/760/remote/341800/screenshots")))),
                            new DIV { class_ = "json" }._(new A { href = "/json", accesskey = "j" }._("See JSON".Accel('J'))),
                            new DIV { class_ = "icon-credits" }._("Module icons by lumbud84, samfun123 and Mushy.")),
                        new DIV { id = "legal" }._(new A { href = "https://legal.timwi.de" }._("Legal stuff · Impressum · Datenschutzerklärung"))))));
        }
    }
}
