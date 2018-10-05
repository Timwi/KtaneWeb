using System;
using System.Linq;
using System.Reflection;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        // Access keys:
        // A    Logfile Analyzer
        // B    sort by Twitch Plays score
        // C    link to Source code
        // D    sort by date published
        // E    sort by expert difficulty
        // F    Find
        // G    Glossary
        // H    link to Steam Workshop item
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
        // S    Rule seed
        // T    link to Tutorial video
        // U    link to Manual
        // V    include/exclude vanilla
        // W    include/exclude widgets
        // X
        // Y    include/exclude needy modules
        // Z
        // .    Filters

        static KtaneFilter[] _filters = Ut.NewArray(
            KtaneFilter.Checkboxes("origin", "Origin", mod => mod.Origin, @"mod=>mod.Origin"),
            KtaneFilter.Checkboxes("type", "Type", mod => mod.Type, @"mod=>mod.Type"),
            KtaneFilter.Slider("defdiff", "Defuser difficulty", mod => mod.DefuserDifficulty, @"mod=>mod.DefuserDifficulty"),
            KtaneFilter.Slider("expdiff", "Expert difficulty", mod => mod.ExpertDifficulty, @"mod=>mod.ExpertDifficulty"),
            KtaneFilter.Checkboxes("twitchplays", "Twitch Plays", mod => mod.TwitchPlaysSupport, @"mod=>mod.TwitchPlaysSupport"),
            KtaneFilter.Checkboxes("ruleseed", "Rule seed", mod => mod.RuleSeedSupport, @"mod=>mod.RuleSeedSupport||'NotSupported'"),
            KtaneFilter.Checkboxes("souvenir", "Souvenir", mod => mod.Souvenir == null ? KtaneModuleSouvenir.NotACandidate : mod.Souvenir.Status, @"mod=>mod.Souvenir?mod.Souvenir.Status:""NotACandidate"""));

        static Selectable[] _selectables = Ut.NewArray(
            new Selectable
            {
                HumanReadable = "Manual",
                Accel = 'u',
                IconFunction = @"(_,s)=>el('img', 'icon manual-icon', { title: 'Manual', alt: 'Manual', src: s[0]['icon'] })",
                DataAttributeName = "manual",
                DataAttributeFunction = @"(_,s)=>s",
                HasValue = m => true,
                UrlFunction = @"(_,s)=>s.length?s[0]['url']:null",
                ShowIconFunction = @"(_,s)=>!!s.length",
                CssClass = "manual"
            },
            new Selectable
            {
                HumanReadable = "Steam Workshop",
                Accel = 'h',
                Icon = "HTML/img/steam-workshop-item.png",
                DataAttributeName = "steam",
                DataAttributeFunction = @"mod=>mod.SteamID?`http://steamcommunity.com/sharedfiles/filedetails/?id=${mod.SteamID}`:null",
                UrlFunction = @"mod=>`http://steamcommunity.com/sharedfiles/filedetails/?id=${mod.SteamID}`",
                ShowIconFunction = @"mod=>!!mod.SteamID",
                HasValue = m => m.SteamID != null
            },
            new Selectable
            {
                HumanReadable = "Source code",
                Accel = 'c',
                Icon = "HTML/img/unity.png",
                DataAttributeName = "source",
                DataAttributeFunction = @"mod=>mod.SourceUrl",
                UrlFunction = @"mod=>mod.SourceUrl",
                ShowIconFunction = @"mod=>!!mod.SourceUrl",
                HasValue = m => m.SourceUrl != null
            },
            new Selectable
            {
                HumanReadable = "Tutorial video",
                Accel = 'T',
                Icon = "HTML/img/video.png",
                DataAttributeName = "video",
                DataAttributeFunction = @"mod=>mod.TutorialVideoUrl",
                UrlFunction = @"mod=>mod.TutorialVideoUrl",
                ShowIconFunction = @"mod=>!!mod.TutorialVideoUrl",
                HasValue = m => m.TutorialVideoUrl != null
            });

        private HttpResponse mainPage(HttpRequest req)
        {
            var displays = Ut.NewArray(
                new { Readable = "Description", Id = "description" },
                //new { Readable = "Author", Id = "author" },
                //new { Readable = "Type", Id = "type" },
                new { Readable = "Difficulty", Id = "difficulty" },
                new { Readable = "Origin", Id = "origin" },
                new { Readable = "Twitch support", Id = "twitch" },
                new { Readable = "Souvenir support", Id = "souvenir" },
                new { Readable = "Rule seed support", Id = "rule-seed" },
                new { Readable = "Date published", Id = "published" },
                new { Readable = "Module ID", Id = "id" });

            var cssLink = req.Url.WithParent("css");
#if DEBUG
            cssLink = cssLink.WithQuery("u", DateTime.UtcNow.Ticks.ToString());
#endif

            var resp = HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages"),
                    new LINK { href = req.Url.WithParent("HTML/css/font.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = cssLink.ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("HTML/css/dark-theme.css").ToHref(), id = "theme-css", rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "HTML/js/jquery.3.1.1.min.js" },
                    new SCRIPT { src = "HTML/js/jquery-ui.1.12.1.min.js" },
                    new LINK { href = req.Url.WithParent("HTML/css/jquery-ui.1.12.1.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPTLiteral(@"Ktane = { Themes: { 'dark': 'HTML/css/dark-theme.css' } };"),
                    new SCRIPT { src = req.Url.WithParent("js").ToHref() },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" }),
                new BODY(
                    new DIV { id = "main-content" }._(
                        new DIV { id = "logo" }._(new IMG { src = "HTML/img/repo-logo.png" }),
                        new DIV { id = "icons", class_ = "icons" }._(
                            new DIV { class_ = "icon-page shown" }._(
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "More/Logfile%20Analyzer.html", accesskey = "a" }._(new IMG { class_ = "icon-img", src = "HTML/img/logfile-analyzer.png" }, new SPAN { class_ = "icon-label" }._("Logfile Analyzer".Accel('A')))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link popup-link", href = "More/Profile%20Editor.html", id = "profiles-link" }.Data("popup", "profiles")._(new IMG { class_ = "icon-img", src = "HTML/img/profile-editor.png" }, new SPAN { class_ = "icon-label", id = "profiles-rel" }._("Profiles"))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "https://discord.gg/Fv7YEDj" }._(new IMG { class_ = "icon-img", src = "HTML/img/discord.png" }, new SPAN { class_ = "icon-label" }._("Join us on Discord"))),
                                new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link", href = "/More/FAQs.html", id = "faq-link-mobile" }._(new IMG { class_ = "icon-img", src = "HTML/img/faq.png" }, new SPAN { class_ = "icon-label" }._("Glossary"))),
                                new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link popup-link", href = "#", id = "rule-seed-link-mobile" }.Data("popup", "rule-seed")._(new IMG { class_ = "icon-img", src = "HTML/img/spanner.png" }, new SPAN { class_ = "icon-label" }._("Rule seed"))),
                                new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link popup-link", href = "#", id = "filters-link-mobile" }.Data("popup", "filters")._(new IMG { class_ = "icon-img", src = "HTML/img/filter-icon.png" }, new SPAN { class_ = "icon-label" }._("Filters"))),
                                new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link popup-link", href = "#", id = "more-link-mobile" }.Data("popup", "more")._(new IMG { class_ = "icon-img", src = "HTML/img/more.png" }, new SPAN { class_ = "icon-label" }._("More")))),
                            new DIV { class_ = "icon-page" }._(
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "More/On%20the%20Subject%20of%20Entering%20the%20World%20of%20Mods.html" }._(new IMG { class_ = "icon-img", src = "HTML/img/google-docs.png" }, new SPAN { class_ = "icon-label" }._("Intro to Playing with Mods"))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "https://docs.google.com/document/d/1fFkBprpo1CMy-EJ-TyD6C_NoX1_7kgiOFeCRdBsh6hk/edit?usp=sharing" }._(new IMG { class_ = "icon-img", src = "HTML/img/google-docs.png" }, new SPAN { class_ = "icon-label" }._("Intro to Making Mods"))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "https://www.youtube.com/playlist?list=PL-1P5EmkkFxrAXBhqvyUAXH-ErGjh7Zrx" }._(new IMG { class_ = "icon-img", src = "HTML/img/video-playlist.png" }, new SPAN { class_ = "icon-label" }._("Tutorial videos playlist")))),
                            new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "#", id = "icon-page-next" }._(new IMG { class_ = "icon-img", src = "HTML/img/more.png" }, new SPAN { class_ = "icon-label" }._("More")))),

                        new A { href = "#", class_ = "mobile-opt", id = "page-opt" },

                        new DIV { id = "top-controls" }._(
                            new DIV { class_ = "search-container" }._(
                                new LABEL { for_ = "search-field" }._("Find: ".Accel('F')),
                                new INPUT { type = itype.text, id = "search-field", accesskey = "f" }, " ",
                                new SCRIPTLiteral("document.getElementById('search-field').focus();"),
                                new A { href = "#", id = "search-field-clear" },
                                new DIV { class_ = "search-options" }._(
                                    new SPAN { class_ = "search-option", id = "search-opt-names" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-names" }, new LABEL { for_ = "search-names" }._("Names")),
                                    new SPAN { class_ = "search-option", id = "search-opt-authors" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-authors" }, new LABEL { for_ = "search-authors" }._("Authors")),
                                    new SPAN { class_ = "search-option", id = "search-opt-descriptions" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-descriptions" }, new LABEL { for_ = "search-descriptions" }._("Descriptions")))),
                            new DIV { id = "rule-seed-mobile", class_ = "popup-link" }.Data("popup", "rule-seed")),

                        new DIV { id = "main-table-container" }._(
                            new DIV { id = "tabs" }._(
                                new A { href = "#", class_ = "tab popup-link", id = "rule-seed-link", accesskey = "s" }.Data("popup", "rule-seed")._("Rule seed".Accel('s'), new SPAN { id = "rule-seed-number" }),
                                new A { href = "/More/FAQs.html", class_ = "tab", id = "faq-link", accesskey = "g" }._("Glossary".Accel('G')),
                                new A { href = "#", class_ = "tab popup-link", id = "filters-link", accesskey = "." }.Data("popup", "filters")._("Filters"),
                                new A { href = "#", class_ = "tab popup-link", id = "more-link" }.Data("popup", "more")._("More")),
                            new TABLE { id = "main-table" }._(
                                new TR { class_ = "header-row" }._(
                                    new TH { colspan = _selectables.Length }._("Links"),
                                    new TH { class_ = "modlink" }._(new A { href = "#", class_ = "sort-header" }._("Name")),
                                    new TH { class_ = "infos" }._(new A { href = "#", class_ = "sort-header" }._("Information"))))),
                        new DIV { id = "module-count" },
                        new DIV { id = "legal" }._(new A { href = "https://legal.timwi.de" }._("Legal stuff · Impressum · Datenschutzerklärung")),

                        new DIV { id = "profiles", class_ = "popup disappear stay" }._(
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

                        new DIV { id = "rule-seed", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new P { class_ = "ui" }._(
                                "Rule seed: ",
                                new INPUT { type = itype.number, step = "1", id = "rule-seed-input", value = "1", class_ = "focus-on-show" }),
                            new P { class_ = "explain" }._("Varies the rules/manuals for supported modules."),
                            new P { class_ = "explain" }._("Requires the ", new A { href = "https://steamcommunity.com/sharedfiles/filedetails/?id=1224413364" }._("Rule Seed Modifier"), " mod."),
                            new P { class_ = "explain" }._("Set to 1 to revert to default rules.")),

                        new DIV { id = "filters", class_ = "popup disappear stay" }._(
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
                                _filters.Select(filter => new DIV { class_ = "filter " + filter.DataAttributeName }._(filter.ToHtml())),
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
                                        new INPUT { id = "sort-twitch-score", name = "sort", value = "twitchscore", class_ = "sort", type = itype.radio },
                                        new LABEL { for_ = "sort-twitch-score", accesskey = "b" }._("\u00a0Sort by score on TP:KTANE".Accel('b'))),
                                    new DIV(
                                        new INPUT { id = "sort-published", name = "sort", value = "published", class_ = "sort", type = itype.radio },
                                        new LABEL { for_ = "sort-published", accesskey = "d" }._("\u00a0Sort by date published".Accel('d')))),
                                new DIV { class_ = "link-targets" }._(
                                    new H4("Make links go to:"),
                                    _selectables.Select(sel => new DIV(
                                        new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{sel.DataAttributeName}" }.Data("selectable", sel.DataAttributeName), " ",
                                        new LABEL { class_ = "set-selectable", id = $"selectable-label-{sel.DataAttributeName}", for_ = $"selectable-{sel.DataAttributeName}", accesskey = sel.Accel?.ToString().ToLowerInvariant() }._(sel.HumanReadable.Accel(sel.Accel)))),
                                    new DIV { id = "include-missing" }._(
                                        new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-include-missing" }, " ",
                                        new LABEL { for_ = "filter-include-missing", accesskey = "i" }._("Include missing".Accel('I')))),
                                new DIV { class_ = "profile none-selected" }._(
                                    new H4("Profile:"),
                                    new DIV { class_ = "filter-profile" }._(
                                        new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-profile-enabled" },
                                        new LABEL { for_ = "filter-profile-enabled", class_ = "filter-profile-enabled-text" }),
                                    new DIV { class_ = "filter-profile" }._(
                                        new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-profile-disabled" },
                                        new LABEL { for_ = "filter-profile-disabled", class_ = "filter-profile-disabled-text" }),
                                    new DIV { class_ = "filter-profile upload" }._(
                                        new INPUT { type = itype.file, id = "profile-file", style = "display: none" },
                                        new LABEL { for_ = "profile-file" }._("Filter by a profile"))))),

                        new DIV { id = "more", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            !_pdfEnabled ? null : new DIV { class_ = "pdf-merge" }._(
                                new FORM { action = "merge-pdf", method = method.post }._(
                                new INPUT { type = itype.hidden, name = "json", id = "generate-pdf-json" },
                                new BUTTON { id = "generate-pdf", type = btype.submit }._("Download merged PDF for current filter"))),
                            new UL { class_ = "dev" }._(
                                new LI(new A { href = "More/Periodic Table.html" }._("Periodic Table of Modules")),
                                new LI(new A { href = "More/Experting Template.png" }._("Experting template"), new DIV { class_ = "link-extra" }._("(printable page with boxes to fill in while experting)")),
                                new LI(new A { href = "https://form.jotform.com/62686042776162" }._("Submit an idea for a new mod")),
                                new LI(new A { href = "https://form.jotform.com/62718595122156" }._("Find a mod idea to implement")),
                                new LI(new A { href = "More/Mode Settings Editor.html" }._("Mode Settings Editor")),
                                new LI(new A { href = "https://github.com/Timwi/KtaneContent" }._("KtaneContent github repository"), new DIV { class_ = "link-extra" }._("(contains the manuals, Profile Editor, Logfile Analyzer and other static files)")),
                                new LI(new A { href = "https://github.com/Timwi/KtaneWeb" }._("KtaneWeb github repository"), new DIV { class_ = "link-extra" }._("(contains this website’s server code)"))),
                            new DIV { class_ = "highlighting-controls" }._(
                                new H3("Controls to highlight elements in HTML manuals"),
                                new TABLE { class_ = "highlighting-controls" }._(
                                    new TR(new TH("Highlight a table column"), new TD("Ctrl+Click (Windows)", new BR(), "Command+Click (Mac)")),
                                    new TR(new TH("Highlight a table row"), new TD("Shift+Click")),
                                    new TR(new TH("Highlight a table cell or an item in a list"), new TD("Alt+Click (Windows)", new BR(), "Ctrl+Shift+Click (Windows)", new BR(), "Command+Shift+Click (Mac)")),
                                    new TR(new TH("Change highlighter color"), new TD("Alt+1, Alt+2, Alt+3, Alt+4")))),
                            new H3("Default file locations"),
                            new H4("Windows"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH("Game:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes" })),
                                new TR(new TH("Logfile (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt" })),
                                new TR(new TH("Logfile (Oculus):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Oculus\Software\steel-crate-games-keep-talking-and-nobody-explodes\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt" })),
                                new TR(new TH("Mod Selector Profiles:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\ModProfiles" })),
                                new TR(new TH("Mod Settings:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\Modsettings" })),
                                new TR(new TH("Screenshots (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Steam\userdata\<some number>\760\remote\341800\screenshots" }))),
                            new H4("Mac"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH("Game:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/Steam/steamapps/common/Keep Talking and Nobody Explodes" })),
                                new TR(new TH("Logfile:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Logs/Unity/Player.log" })),
                                new TR(new TH("Mod Selector Profiles:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/com.steelcrategames.keeptalkingandnobodyexplodes/ModProfiles" })),
                                new TR(new TH("Mod Settings:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/com.steelcrategames.keeptalkingandnobodyexplodes/Modsettings" })),
                                new TR(new TH("Screenshots (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/Steam/userdata/<some number>/760/remote/341800/screenshots" }))),
                            new H4("Linux"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH("Game:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.steam/steamapps/common/Keep Talking and Nobody Explodes" })),
                                new TR(new TH("Logfile:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/Player.log" })),
                                new TR(new TH("Mod Selector Profiles:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/ModProfiles" })),
                                new TR(new TH("Mod Settings:"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/Modsettings" })),
                                new TR(new TH("Screenshots (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.steam/userdata/<some number>/760/remote/341800/screenshots" }))),
                            new DIV { class_ = "json" }._(new A { href = "/json", accesskey = "j" }._("See JSON".Accel('J')))),

                        new DIV { id = "page-opt-popup", class_ = "popup disappear stay" }._(new DIV { class_ = "close" }),

                        new Func<object>(() =>
                        {
                            var modules = _config.Current.KtaneModules.Select(mod => new JsonDict
                            {
                                ["m"] = ClassifyJson.Serialize(mod),
                                ["s"] = _config.EnumerateSheetUrls(mod.Name, _config.Current.KtaneModules.Select(m => m.Name).Where(m => m.Length > mod.Name.Length && m.StartsWith(mod.Name)).ToArray())
                            }).ToJsonList();
                            var iconDirs = Enumerable.Range(0, _config.DocumentDirs.Length).SelectMany(ix => new[] { _config.OriginalDocumentIcons[ix], _config.ExtraDocumentIcons[ix] }).ToJsonList();
                            var disps = displays.Select(d => d.Id).ToJsonList();
                            var filters = _filters.Select(f => f.ToJson()).ToJsonList();
                            var selectables = _selectables.Select(sel => sel.ToJson()).ToJsonList();
                            var souvenir = EnumStrong.GetValues<KtaneModuleSouvenir>().ToJsonDict(val => val.ToString(), val => val.GetCustomAttribute<KtaneSouvenirInfoAttribute>().Apply(attr => new JsonDict { { "Tooltip", attr.Tooltip }, { "Char", attr.Char.ToString() } }));
                            ensureIconSpriteInfo();
                            foreach (var module in modules)
                            {
                                var name = module["m"]["Name"].GetString();
                                if (_iconSpriteInfo.CoordsJson.ContainsKey(name))
                                    module["m"]["Icon"] = _iconSpriteInfo.CoordsJson[name];
                            }
                            return new SCRIPTLiteral($@"initializePage({modules},{iconDirs},{_config.DocumentDirs.ToJsonList()},{disps},{filters},{selectables},{souvenir},{JsonValue.ToString(_iconSpriteInfo.CoordsJson.Get("blank", null))});");
                        })))));
            resp.UseGzip = UseGzipOption.DontUseGzip;
            return resp;
        }
    }
}
