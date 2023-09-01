using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RT.Json;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        // Access keys:
        // A    Logfile Analyzer
        // B    sort by Twitch Plays score
        // C    link to Source code
        // D    sort by date published
        // E    Reverse sort
        // F    Find
        // G    Glossary
        // H
        // I    toggle views
        // J    Generate JSON submit button (module edit UI)
        // K    Dark Theme
        // L    Light Theme
        // M    include/exclude mods
        // N    sort by name
        // O    sort by Time Mode score
        // P    Profile Editor
        // Q    
        // R    include/exclude regular modules
        // S    Rule seed
        // T    link to Tutorial video
        // U    link to Manual
        // V    include/exclude vanilla
        // W    link to Steam Workshop item
        // X
        // Y    include/exclude needy modules
        // Z
        // ,    Switch between Find box and Missions drop-down
        // .    Filters
        // /    Options

        private static string UniquifiedUrl(IHttpUrl url)
        {
#if DEBUG
            return url.WithQuery("uniq", DateTime.UtcNow.Ticks.ToString()).ToHref();
#else
            return url.ToHref();
#endif
        }

        private HttpResponse mainPage(HttpRequest req)
        {
            var lang = req.Url["lang"] ?? req.Headers.Cookie.Get("lang", null)?.Value ?? "en";
            var translation = _translationCache.Get(lang, TranslationInfo.Default);
            var resp = HttpResponse.Html(new HTML { lang = translation.langCode }._(
                new HEAD(
                    new TITLE(translation.title),
                    new META { name = "description", content = "Manuals for Keep Talking and Nobody Explodes — vanilla, modded, optimized/embellished, logfile analyzer, profile editor and more" },
                    new LINK { href = req.Url.WithParent("HTML/css/font.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = UniquifiedUrl(req.Url.WithParent("css")), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("HTML/css/dark-theme.css").ToHref(), id = "theme-css", rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "HTML/js/jquery.3.7.0.min.js" },
                    new SCRIPT { src = "HTML/js/jquery-ui.1.13.2.min.js" },
                    new LINK { href = req.Url.WithParent("HTML/css/jquery-ui.1.12.1.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPTLiteral($@"Ktane = {{
                        Themes: {{ 'dark': 'HTML/css/dark-theme.css' }},
                        Languages: {{ {TranslationInfo.LanguageCodeToName.Select(kvp => $"{kvp.Key.JsEscape()}: {kvp.Value.JsEscape()}").JoinString(", ")} }},
                        InitDisplays: [{translation.Displays.Select(d => d.id.JsEscape()).JoinString(", ")}]
                    }};"),
                    new SCRIPT { src = UniquifiedUrl(req.Url.WithParent("js")) },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new STYLELiteral($@"
                        div.infos .inf-author::before {{ content: '\000a {cssEscape(translation.by)} '; }}
                        #main-table .manual-selector::before {{ content: '{cssEscape(translation.more)}'; }}
                        td.infos-1 div.infos > .inf-description > .inf-tags::before {{ content: '{cssEscape(translation.tags)}'; }}
                        body.sort-name #main-table th.modlink::after {{ content: '   • {cssEscape(translation.sortOrderName)}'; }}
                        body.sort-defdiff #main-table th.infos::after {{ content: '   • {cssEscape(translation.sortOrderDefDifficulty)}'; }}
                        body.sort-expdiff #main-table th.infos::after {{ content: '   • {cssEscape(translation.sortOrderExpDifficulty)}'; }}
                        body.sort-twitch-score #main-table th.infos::after {{ content: '   • {cssEscape(translation.sortOrderTP)}'; }}
                        body.sort-time-mode-score #main-table th.infos::after {{ content: '   • {cssEscape(translation.sortOrderTime)}'; }}
                        body.sort-published #main-table th.infos::after {{ content: '   • {cssEscape(translation.sortOrderDate)}'; }}
                        @media screen and (max-width: 1090px) and (min-width: 650.01px) {{
                            body.sort-name th.modlink::after {{ content: '   • {cssEscape(translation.sortOrderName)}'; }}
                            body.sort-defdiff th.infos::after {{ content: '   • {cssEscape(translation.sortOrderDefDifficulty)}'; }}
                            body.sort-expdiff th.infos::after {{ content: '   • {cssEscape(translation.sortOrderExpDifficulty)}'; }}
                            body.sort-twitch-score th.infos::after {{ content: '   • {cssEscape(translation.sortOrderTP)}'; }}
                            body.sort-time-mode-score th.infos::after {{ content: '   • {cssEscape(translation.sortOrderTime)}'; }}
                            body.sort-published th.infos::after {{ content: '   • {cssEscape(translation.sortOrderDate)}'; }}
                        }}")),
                new BODY(
                    new DIV { id = "main-content" }._(
                        new DIV { class_ = "header" }._(

                            new IMG { id = "logo", src = translation.titleImg },
                            new DIV { id = "icons", class_ = "icons" }._(
                                /* LANGUAGE SELECTOR */
                                new DIV { class_ = "lang" }._(
                                    new SPAN { class_ = "lang-label" }._(translation.langSelectorLabel + ":"),
                                    new SELECT { class_ = "lang-selector", id = "lang-selector" }
                                ),
                                new DIV { class_ = "icon-page shown" }._(
                                    new DIV { class_ = "icon", id = "links-rel" }._(new A { class_ = "icon-link popup-link", href = "#" }.Data("popup", "links")._(new IMG { class_ = "icon-img", src = "HTML/img/links-icon.png" }, new SPAN { class_ = "icon-label" }._(translation.popupLinks))),
                                    new DIV { class_ = "icon", id = "tools-rel" }._(new A { class_ = "icon-link popup-link", href = "#" }.Data("popup", "tools")._(new IMG { class_ = "icon-img", src = "HTML/img/logfile-analyzer.png" }, new SPAN { class_ = "icon-label" }._(translation.popupTools))),
                                    new DIV { class_ = "icon", id = "view-rel" }._(new A { class_ = "icon-link popup-link", href = "#" }.Data("popup", "view")._(new IMG { class_ = "icon-img", src = "HTML/img/view-icon.png" }, new SPAN { class_ = "icon-label" }._(translation.popupView))),
                                    new DIV { class_ = "icon", id = "more-rel" }._(new A { class_ = "icon-link popup-link", href = "#" }.Data("popup", "more")._(new IMG { class_ = "icon-img", src = "HTML/img/more.png" }, new SPAN { class_ = "icon-label" }._(translation.popupMore))),
                                    new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link popup-link", href = "#", id = "rule-seed-link-mobile" }.Data("popup", "rule-seed")._(new IMG { class_ = "icon-img", src = "HTML/img/spanner.png" }, new SPAN { class_ = "icon-label" }._(translation.tabRuleSeed))),
                                    new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link popup-link", href = "#", id = "filters-link-mobile" }.Data("popup", "filters")._(new IMG { class_ = "icon-img", src = "HTML/img/filter-icon.png" }, new SPAN { class_ = "icon-label" }._(translation.tabFilters))),
                                    new DIV { class_ = "icon mobile-only" }._(new A { class_ = "icon-link popup-link", href = "#", id = "options-link-mobile" }.Data("popup", "options")._(new IMG { class_ = "icon-img", src = "HTML/img/sliders.png" }, new SPAN { class_ = "icon-label" }._(translation.tabOptions)))))
                        ),

                        new A { href = "#", class_ = "mobile-opt", id = "page-opt" },

                        // SEARCH FIELD (and rule seed display on mobile)
                        new DIV { id = "top-controls" }._(
                            new A { id = "search-switcher", href = "#", accesskey = "," },
                            new DIV { class_ = "search-container visible" }._(
                                new LABEL { for_ = "search-field" }._((translation.searchFind + " ").Accel('F')),
                                new INPUT { type = itype.text, id = "search-field", class_ = "sw-focus", accesskey = "f" }, " ",
                                new SCRIPTLiteral("document.getElementById('search-field').focus();"),
                                new A { href = "#", class_ = "search-field-clear" },
                                new DIV { class_ = "search-options" }._(
                                    new SPAN { class_ = "search-option", id = "search-opt-names" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-names" }, new LABEL { for_ = "search-names" }._(translation.searchNames)),
                                    new SPAN { class_ = "search-option", id = "search-opt-authors" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-authors" }, new LABEL { for_ = "search-authors" }._(translation.searchAuthors)),
                                    new SPAN { class_ = "search-option", id = "search-opt-descriptions" }._(new INPUT { type = itype.checkbox, class_ = "search-option-input", id = "search-descriptions" }, new LABEL { for_ = "search-descriptions" }._(translation.searchDescriptions)))),
                            new DIV { class_ = "search-container" }._(
                                new LABEL { for_ = "search-field-mission" }._(translation.searchMission + " "),
                                new SELECT { id = "search-field-mission", class_ = "sw-focus" }, " ",
                                new A { id = "search-field-mission-link", accesskey = "]" }._("open")),
                            new DIV { id = "rule-seed-mobile", class_ = "popup-link" }.Data("popup", "rule-seed"),

                            // TABS
                            new DIV { id = "tabs" }._(
                                new A { href = "#", class_ = "tab popup-link", id = "rule-seed-link", accesskey = "s" }.Data("popup", "rule-seed")._(translation.tabRuleSeed.Accel('s'), new SPAN { id = "rule-seed-number" }),
                                new A { href = "#", class_ = "tab popup-link", id = "filters-link", accesskey = "." }.Data("popup", "filters")._(translation.tabFilters),
                                new A { href = "#", class_ = "tab popup-link", id = "options-link", accesskey = "/" }.Data("popup", "options")._(translation.tabOptions))),

                        new DIV { id = "main-table-container" }._(
                            // MAIN TABLE
                            new TABLE { id = "main-table" }._(
                                new TR { class_ = "header-row" }._(
                                    new TH { colspan = translation.Selectables.Length }._(translation.columnLinks),
                                    new TH { class_ = "modlink" }._(new A { href = "#", class_ = "sort-header" }._(translation.columnName)),
                                    new TH { class_ = "infos" }._(new A { href = "#", class_ = "sort-header" }._(translation.columnInformation)))),

                            // PERIODIC TABLE
                            new DIV { id = "main-periodic-table" }._(
                                new DIV { id = "actual-periodic-table" })),
                        new DIV { id = "module-count" },

                        new DIV { id = "legal" }._(new A { href = "https://legal.timwi.de" }._("Legal stuff · Impressum · Datenschutzerklärung")),

                        // LINKS (icon popup)
                        new DIV { id = "links", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "icons" }._(
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "https://discord.gg/K6uQMyBcYZ" }._(new IMG { class_ = "icon-img", src = "HTML/img/discord.png" }, new SPAN { class_ = "icon-label" }._(translation.joinDiscordAnchor))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = translation.glossaryURL, accesskey = "g" }._(new IMG { class_ = "icon-img", src = "HTML/img/faq.png" }, new SPAN { class_ = "icon-label" }._(translation.glossaryAnchor.Accel('G')))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = translation.tutorialURL }._(new IMG { class_ = "icon-img", src = "HTML/img/google-docs.png" }, new SPAN { class_ = "icon-label" }._(translation.tutorialAnchor))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "More/On%20the%20Subject%20of%20Making%20a%20Great%20Module.html" }._(new IMG { class_ = "icon-img", src = "HTML/img/google-docs.png" }, new SPAN { class_ = "icon-label" }._(translation.makingModsAnchor))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "https://www.youtube.com/playlist?list=PL-1P5EmkkFxrAXBhqvyUAXH-ErGjh7Zrx" }._(new IMG { class_ = "icon-img", src = "HTML/img/video-playlist.png" }, new SPAN { class_ = "icon-label" }._(translation.playlistAnchor)))),
                            new UL { class_ = "below-icons" }._(
                                new LI(new A { href = "More/Repository%20Symbols%20Guide.html" }._(translation.symbolGuideAnchor)),
                                new LI(new A { href = "https://ktane.onpointcoding.net/ideas/" }._(translation.modIdeaAnchor)),
                                new LI(new A { href = "https://docs.google.com/spreadsheets/d/10Z7Ivc784QaFrQCaGwIPUYrS6NNXiLJPi8nADiFR_0s" }._(translation.modIdeaPastAnchor)),
                                new LI(new A { href = "https://www.reddit.com/r/ktanemod/" }._(translation.modIdeaSubredditAnchor)),
                                new LI(new A { href = "https://github.com/Timwi/KtaneContent" }._(translation.contentGithubAnchor), new DIV { class_ = "link-extra" }._(translation.contentGithubDesc)),
                                new LI(new A { href = "https://github.com/Timwi/KtaneWeb" }._(translation.webGithubAnchor), new DIV { class_ = "link-extra" }._(translation.webGithubDesc)))),

                        // TOOLS (icon popup)
                        new DIV { id = "tools", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "icons" }._(
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "More/Logfile%20Analyzer.html", accesskey = "a" }._(new IMG { class_ = "icon-img", src = "HTML/img/logfile-analyzer.png" }, new SPAN { class_ = "icon-label" }._(translation.lfaAnchor.Accel('A')))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "More/Profile%20Editor.html", accesskey = "p" }._(new IMG { class_ = "icon-img", src = "HTML/img/profile-editor.png" }, new SPAN { class_ = "icon-label" }._(translation.profileEditorAnchor))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "profile/zip" }._(new IMG { class_ = "icon-img", src = "HTML/img/profile-editor.png" }, new SPAN { class_ = "icon-label" }._(translation.downloadProfileAnchor.Accel('p')))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "More/Mode%20Settings%20Editor.html" }._(new IMG { class_ = "icon-img", src = "HTML/img/profile-editor.png" }, new SPAN { class_ = "icon-label" }._(translation.modeEditorAnchor))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link", href = "#", id = "module-json-new" }._(new IMG { class_ = "icon-img", src = "HTML/img/edit-icon.png" }, new SPAN { class_ = "icon-label" }._(translation.newModuleAnchor)))),
                            new DIV { class_ = "pdf-merge" }._(
                                new FORM { action = "merge-pdf", method = method.post }._(
                                new INPUT { type = itype.hidden, name = "json", id = "generate-pdf-json" },
                                new BUTTON { id = "generate-pdf", type = btype.submit }._(translation.downloadPDF))),
                            new UL { class_ = "below-icons" }._(
                                new LI(new A { href = translation.ignoreTableURL }._(translation.ignoredTableAnchor)),
                                new LI(new A { href = "https://files.timwi.de/Tools/Calculator.html" }._(translation.tfcAnchor)))),

                        // VIEW (icon popup)
                        new DIV { id = "view", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "icons" }._(
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link view-link", href = "#" }.Data("view", "List")._(new IMG { class_ = "icon-img", src = "HTML/img/list-icon.png" }, new SPAN { class_ = "icon-label" }._(translation.displayMethodList))),
                                new DIV { class_ = "icon" }._(new A { class_ = "icon-link view-link", href = "#" }.Data("view", "PeriodicTable")._(new IMG { class_ = "icon-img", src = "HTML/img/grid-icon.png" }, new SPAN { class_ = "icon-label" }._(translation.displayMethodPeriodic))))),

                        // MORE (icon popup)
                        new DIV { id = "more", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new UL { class_ = "below-icons first" }._(
                                new LI(new A { href = "puzzles", class_ = "important" }._(translation.puzzleAnchor)),
                                new LI(new A { href = translation.quizURL, class_ = "important" }._(translation.quizAnchor)),
                                new LI(new A { href = "More/Experting%20Template.png" }._(translation.expertTemplateAnchor), new DIV { class_ = "link-extra" }._(translation.expertTemplateDesc)),
                                new LI(new A { href = "More/Template%20Manual.zip" }._(translation.templateManualAnchor), new DIV { class_ = "link-extra" }._(translation.templateManualDesc)),
                                new LI(new A { href = "More/DeMiLMissionViewer/index.html" }._(translation.demilAnchor), new DIV { class_ = "link-extra" }._(translation.demilDesc))),
                            new DIV { class_ = "highlighting-controls" }._(
                                new H3(translation.controlHeader),
                                new TABLE { class_ = "highlighting-controls" }._(
                                    translation.controls.Select(c1 => new TR()._(
                                        new TH(c1.Length > 0 ? c1[0] : null),
                                        new TD()._(c1.Skip(1).SelectMany(c2 => new List<object>() { c2, new BR() }).SkipLast(1))
                                    ))
                                )
                            ),
                            new H3(translation.fileLocationHeader),
                            new H4("Windows"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH(translation.fileLocationGame + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes" })),
                                new TR(new TH(translation.fileLocationLogfile + " (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\output_log.txt" })),
                                new TR(new TH(translation.fileLocationLogfile + " (Oculus):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Oculus\Software\steel-crate-games-keep-talking-and-nobody-explodes\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt" })),
                                new TR(new TH(translation.fileLocationProfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\ModProfiles" })),
                                new TR(new TH(translation.fileLocationSetting + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"%APPDATA%\..\LocalLow\Steel Crate Games\Keep Talking and Nobody Explodes\Modsettings" })),
                                new TR(new TH(translation.fileLocationScreenshot + " (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"C:\Program Files (x86)\Steam\userdata\<some number>\760\remote\341800\screenshots" }))),
                            new H4("Mac"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH(translation.fileLocationGame + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/Steam/steamapps/common/Keep Talking and Nobody Explodes" })),
                                new TR(new TH(translation.fileLocationLogfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Logs/Unity/Player.log" })),
                                new TR(new TH(translation.fileLocationProfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/com.steelcrategames.keeptalkingandnobodyexplodes/ModProfiles" })),
                                new TR(new TH(translation.fileLocationSetting + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/com.steelcrategames.keeptalkingandnobodyexplodes/Modsettings" })),
                                new TR(new TH(translation.fileLocationScreenshot + " (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/Library/Application Support/Steam/userdata/<some number>/760/remote/341800/screenshots" }))),
                            new H4("Linux"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH(translation.fileLocationGame + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.steam/steam/steamapps/common/Keep Talking and Nobody Explodes" })),
                                new TR(new TH(translation.fileLocationLogfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/Player.log" })),
                                new TR(new TH(translation.fileLocationProfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/ModProfiles" })),
                                new TR(new TH(translation.fileLocationSetting + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.config/unity3d/Steel Crate Games/Keep Talking and Nobody Explodes/Modsettings" })),
                                new TR(new TH(translation.fileLocationScreenshot + " (Steam):"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.steam/steam/userdata/<some number>/760/remote/341800/screenshots" }))),
                            new H4("Steam Deck (Proton)"),
                            new TABLE { class_ = "file-locations" }._(
                                new TR(new TH(translation.fileLocationGame + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.local/share/Steam/steamapps/common/Keep Talking and Nobody Explodes" })),
                                new TR(new TH(translation.fileLocationLogfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.local/share/Steam/steamapps/compatdata/341800/pfx/drive_c/users/steamuser/AppData/LocalLow/Steel Crate Games/Keep Talking and Nobody Explodes/output_log.txt" })),
                                new TR(new TH(translation.fileLocationProfile + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.local/share/Steam/steamapps/compatdata/341800/pfx/drive_c/users/steamuser/AppData/LocalLow/Steel Crate Games/Keep Talking and Nobody Explodes/ModProfiles" })),
                                new TR(new TH(translation.fileLocationSetting + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.local/share/Steam/steamapps/compatdata/341800/pfx/drive_c/users/steamuser/AppData/LocalLow/Steel Crate Games/Keep Talking and Nobody Explodes/Modsettings" })),
                                new TR(new TH(translation.fileLocationScreenshot + ":"), new TD(new INPUT { type = itype.text, class_ = "select-on-focus", value = @"~/.local/share/Steam/userdata/<some number>/760/remote/341800/screenshots" }))),
                            new DIV { class_ = "small-links" }._(_config.DocumentDirs.Select(d => new A { href = d }._(d)).InsertBetween<object>(" • ")),
                            new DIV { class_ = "hidden-shortcuts" }._(new A { href = "#", accesskey = "i", id = "toggle-view" })),

                        // RULE SEED (tab popup)
                        new DIV { id = "rule-seed", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new P { class_ = "ui" }._(
                                translation.ruleSeedLabel, " ",
                                new INPUT { type = itype.number, step = "1", id = "rule-seed-input", value = "1", class_ = "focus-on-show" }),
                            new P { class_ = "explain" }._(translation.ruleSeedExplanation),
                            new P { class_ = "explain" }._(Regex.Match(translation.ruleSeedLink, @"^(.*)\{(.*)\}(.*)$").Apply(m => m.Success
                                ? new object[] { m.Groups[1].Value, new A { href = "https://steamcommunity.com/sharedfiles/filedetails/?id=2037350348" }._(m.Groups[2].Value), m.Groups[3].Value }
                                : (object) translation.ruleSeedLink)),
                            new P { class_ = "explain" }._(translation.ruleSeedDefault)),

                        // FILTERS (tab popup)
                        new DIV { id = "filters", class_ = "popup disappear stay no-profile-selected" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "filters hstack" }._(
                                new DIV { id = "filters-col1", class_ = "vstack" }._(
                                    translation.Filters1.Select(filter => filter.ToHtml(translation))),
                                new DIV { id = "filters-col2", class_ = "vstack" }._(
                                    translation.Filters2.Select(filter => filter.ToHtml(translation)),
                                    new DIV { class_ = "option-group" }._(
                                        new H4(translation.sortOrderHeader),
                                        new DIV(
                                            new INPUT { id = "sort-name", name = "sort", value = "name", class_ = "sort", type = itype.radio },
                                            new LABEL { for_ = "sort-name", accesskey = "n" }._("\u00a0", translation.sortOrderName.Accel('n'))),
                                        new DIV(
                                            new INPUT { id = "sort-defuser-difficulty", name = "sort", value = "defdiff", class_ = "sort", type = itype.radio },
                                            new LABEL { for_ = "sort-defuser-difficulty" }._("\u00a0", translation.sortOrderDefDifficulty)),
                                        new DIV(
                                            new INPUT { id = "sort-expert-difficulty", name = "sort", value = "expdiff", class_ = "sort", type = itype.radio },
                                            new LABEL { for_ = "sort-expert-difficulty" }._("\u00a0", translation.sortOrderExpDifficulty)),
                                        new DIV(
                                            new INPUT { id = "sort-twitch-score", name = "sort", value = "twitchscore", class_ = "sort", type = itype.radio },
                                            new LABEL { for_ = "sort-twitch-score", accesskey = "b" }._("\u00a0", translation.sortOrderTP.Accel('b'))),
                                        new DIV(
                                            new INPUT { id = "sort-time-mode-score", name = "sort", value = "timemodescore", class_ = "sort", type = itype.radio },
                                            new LABEL { for_ = "sort-time-mode-score", accesskey = "o" }._("\u00a0", translation.sortOrderTime.Accel('o'))),
                                        new DIV(
                                            new INPUT { id = "sort-published", name = "sort", value = "published", class_ = "sort", type = itype.radio },
                                            new LABEL { for_ = "sort-published", accesskey = "d" }._("\u00a0", translation.sortOrderDate.Accel('d'))),
                                        new DIV(
                                            new INPUT { id = "sort-reverse", name = "sort", class_ = "sort-reverse", type = itype.checkbox },
                                            new LABEL { for_ = "sort-reverse", accesskey = "e" }._("\u00a0", translation.sortOrderReverse.Accel('e')))),
                                    new DIV { class_ = "option-group" }._(
                                        new H4(translation.filterProfile),
                                        new DIV { class_ = "filter-profile" }._(
                                            new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-profile-enabled" },
                                            new LABEL { for_ = "filter-profile-enabled", class_ = "filter-profile-enabled-text" }),
                                        new DIV { class_ = "filter-profile" }._(
                                            new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-profile-disabled" },
                                            new LABEL { for_ = "filter-profile-disabled", class_ = "filter-profile-disabled-text" }),
                                        new DIV { class_ = "filter-profile upload" }._(
                                            new INPUT { type = itype.file, accept = "application/json", id = "profile-file", style = "display: none" },
                                            new LABEL { for_ = "profile-file" }._(translation.filterProfileOpen))))
                                )),

                        // OPTIONS (tab popup)
                        new DIV { id = "options", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "option-group" }._(
                                new H4(translation.displayOption),
                                translation.Displays.Select(dspl => new DIV(
                                    new INPUT { id = "display-" + dspl.id, name = "display", value = dspl.id, class_ = "display", type = itype.checkbox },
                                    new LABEL { for_ = "display-" + dspl.id }._("\u00a0", dspl.readable)))),
                            new DIV { class_ = "option-group" }._(
                                new H4(translation.searchOption),
                                new DIV(
                                    new INPUT { type = itype.checkbox, class_ = "search-option-checkbox", id = "option-include-steam-id" }, " ",
                                    new LABEL { for_ = "option-include-steam-id" }._(translation.searchSteamID)),
                                new DIV(
                                    new INPUT { type = itype.checkbox, class_ = "search-option-checkbox", id = "option-include-symbol" }, " ",
                                    new LABEL { for_ = "option-include-symbol" }._(translation.searchSymbol)),
                                new DIV(
                                    new INPUT { type = itype.checkbox, class_ = "search-option-checkbox", id = "option-include-module-id" }, " ",
                                    new LABEL { for_ = "option-include-module-id" }._(translation.searchModuleID))),
                            new DIV { class_ = "option-group" }._(
                                new H4(translation.findBarOption),
                                new DIV(
                                    new INPUT { type = itype.radio, class_ = "results-mode", id = "results-hide", name = "results-mode", value = "hide" }, " ",
                                    new LABEL { for_ = "results-hide" }._(translation.findBarMatch),
                                    new DIV { class_ = "sub-option" }._(
                                        new LABEL { for_ = "results-limit" }._(translation.findBarMatchLimit, "\u00a0"),
                                        new INPUT { type = itype.number, id = "results-limit", name = "results-limit", value = "20", step = "1" })),
                                new DIV(
                                    new INPUT { type = itype.radio, class_ = "results-mode", id = "results-scroll", name = "results-mode", value = "scroll" }, " ",
                                    new LABEL { for_ = "results-scroll" }._(translation.findBarScroll))),
                            new DIV { class_ = "option-group" }._(
                                new H4(translation.themeOption),
                                new DIV(
                                    new INPUT { type = itype.radio, class_ = "set-theme", name = "theme", id = "theme-default" }.Data("theme", "null"), " ",
                                    new LABEL { for_ = "theme-default", accesskey = "l" }._(translation.themeLight.Accel('L'))),
                                new DIV(
                                    new INPUT { type = itype.radio, class_ = "set-theme", name = "theme", id = "theme-dark" }.Data("theme", "dark"), " ",
                                    new LABEL { for_ = "theme-dark", accesskey = "k" }._(translation.themeDark.Accel('k')))),
                            new DIV { class_ = "option-group" }._(
                                new H4(translation.linkOption),
                                translation.Selectables.Select(sel => new DIV(
                                    new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{sel.PropName}" }.Data("selectable", sel.PropName), " ",
                                    new LABEL { class_ = "set-selectable", id = $"selectable-label-{sel.PropName}", for_ = $"selectable-{sel.PropName}", accesskey = sel.Accel?.ToString().ToLowerInvariant() }._(sel.HumanReadable.Accel(sel.Accel))))),
                            new DIV { class_ = "option-group" }._(new H4(translation.languagesOption), new DIV { id = "languages-option" }),
                            new BUTTON { class_ = "toggle-all-languages" }._(translation.languagesToggle)),

                        new DIV { id = "page-opt-popup", class_ = "popup disappear stay" }._(new DIV { class_ = "close" }),

                        // Modkit license summary
                        new DIV { id = "license", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "option-group" }._(() =>
                            {
                                Dictionary<string, List<string>> license = new Dictionary<string, List<string>>()
                                {
                                    { "You may:", new List<string>() {
                                        "use the work commercially.",
                                        "make changes to the work.",
                                        "distribute the compiled code and / or source.",
                                        "incorporate the work into something that has a more restrictive license.",
                                        "use the work for private use.",
                                    } },
                                    { "You may not:", new List<string>() {
                                        "hold the author liable.",
                                    } },
                                    { "You must:", new List<string>() {
                                        "include the copyright notice in all copies or substantial uses of the work.",
                                        "include the license notice in all copies or substantial uses of the work.",
                                        "use the work to create mods for KTANE.",
                                    } }
                                };

                                return Ut.NewArray<object>(
                                    new BUTTON { id = "back-to-json" }._("Go Back"),
                                    new P("Quick Summary: The modkit license extends the ", new A { href = "https://tldrlegal.com/license/mit-license" }._("MIT license"), " so you can only use it to create mods for KTANE."),
                                    new TABLE(
                                        new TR(license.Keys.Select(title => new TH(title))),
                                        new TR(license.Values.Select(sentences => new TD(new UL(sentences.Select(sentence => new LI(sentence))))))),
                                    new P("This is not legal advice."));
                            })),

                        // CONTACT INFO
                        new DIV { id = "contact-info", class_ = "popup disappear stay" }._(
                            new DIV { class_ = "close" },
                            new DIV { class_ = "option-group" }._(
                                new H4(translation.contactInformation),
                                new UL())),

                        // Module info editing UI
                        new DIV { id = "module-ui", class_ = "popup disappear stay" }._(new FORM { action = "generate-json", method = method.post }._(new Func<object>(() =>
                        {
                            IEnumerable<object> createTableCellContent(FieldInfo field, EditableFieldAttribute attr)
                            {
                                var type = field.FieldType;
                                if (field.FieldType.TryGetGenericParameters(typeof(Nullable<>), out var types))
                                    type = types[0];

                                yield return new DIV { class_ = "explain" }._(attr.Explanation);
                                if (type.IsEnum && type.GetCustomAttributes<FlagsAttribute>().Any())
                                    foreach (Enum option in Enum.GetValues(type))
                                    {
                                        yield return new INPUT { type = itype.checkbox, name = $"{field.Name}-{option}", id = $"input-{field.Name}-{option}" };
                                        yield return new LABEL { for_ = $"input-{field.Name}-{option}", title = option.GetCustomAttribute<EditableHelpAttribute>()?.Translate(translation) ?? option.ToString() }._("\u00a0", option.GetCustomAttribute<KtaneFilterOptionAttribute>()?.Translate(translation) ?? option.ToString());
                                        yield return new BR();
                                    }
                                else if (type.IsEnum)
                                    yield return new SELECT { name = field.Name }._(Enum.GetValues(type).Cast<Enum>().Select(val => new OPTION { value = val.ToString() }._(val.GetCustomAttribute<KtaneFilterOptionAttribute>()?.Translate(translation) ?? val.ToString())));
                                else if (type == typeof(string) && attr.Multiline)
                                    yield return new TEXTAREA { name = field.Name };
                                else if (type == typeof(string))
                                    yield return new INPUT { type = itype.text, name = field.Name };
                                else if (type == typeof(string[]))
                                    yield return new INPUT { type = itype.text, name = field.Name, class_ = "use-tag-editor" };
                                else if (type == typeof(DateTime))
                                    yield return new INPUT { type = itype.date, value = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), name = field.Name };
                                else if (type == typeof(int))
                                    yield return new INPUT { type = itype.number, step = "1", value = "0", name = field.Name };
                                else if (type == typeof(decimal))
                                    yield return new INPUT { type = itype.number, step = "0.01", value = "0", name = field.Name };
                                else if (type == typeof(Dictionary<string, string>))
                                    yield return new INPUT { type = itype.text, name = field.Name, class_ = "use-dict-editor" }
                                        .Data("AllowedSeparators", new string(attr.AllowedSeparators))
                                        .Data("AllowedDictSeparators", new string(attr.AllowedDictSeparators));
                                else if (type == typeof(bool))
                                {
                                    yield return new INPUT { type = itype.checkbox, name = field.Name, id = $"input-{field.Name}" };
                                    yield return "\u00a0";
                                    yield return new LABEL { for_ = $"input-{field.Name}" }._(attr.ReadableName);
                                }
                                else if (type == typeof(DescriptionInfo[]))
                                    yield return new DIV { class_ = "descriptions" }._(
                                        new INPUT { type = itype.hidden, name = "Descriptions", value = "" },
                                        new TABLE { class_ = "description-list nested" }._(
                                            new THEAD(new TR(new TH("Language"), new TH("Description"), new TH("Tags"))),
                                            new TBODY()),
                                        new DIV { class_ = "description-controls" }._(new BUTTON { id = "description-add", type = btype.button }._("+")));
                                else if (type == typeof(TutorialVideoInfo[]))
                                    yield return new DIV { class_ = "tutorial-videos" }._(
                                        new INPUT { type = itype.hidden, name = "TutorialVideos", value = "" },
                                        new TABLE { class_ = "tutorial-video-list nested" }._(
                                            new THEAD(new TR(new TH("Language"), new TH("Description"), new TH("URL"))),
                                            new TBODY()),
                                        new DIV { class_ = "tutorial-video-controls" }._(new BUTTON { id = "tutorial-video-add", type = btype.button }._("+")));
                                else
                                    yield return new DIV { class_ = "oops" }._("Bug. Please let Timwi know.");

                                if (field.Name == "License")
                                {
                                    yield return new DIV { id = "license-agreement" }._(
                                        new INPUT { type = itype.checkbox, name = "LicenseAgreement", id = "input-LicenseAgreement" },
                                        new LABEL { for_ = "input-LicenseAgreement" }._("I have read and agree to the ", new A { href = "https://github.com/keeptalkinggame/ktanemodkit/blob/master/LICENSE" }._("modkit license"), ". "),
                                        new BUTTON { id = "show-license" }._("See License Summary"));
                                }
                            }

                            IEnumerable<object> iterateNormalFields(Type typeToBeEdited)
                            {
                                foreach (var field in typeToBeEdited.GetFields())
                                {
                                    var attr = field.GetCustomAttribute<EditableFieldAttribute>();
                                    if (attr == null || attr.ReadableName == null || field.GetCustomAttribute<EditableNestedAttribute>() != null)
                                        continue;
                                    var ifAttr = field.GetCustomAttribute<EditableIfAttribute>();
                                    yield return new TR { id = $"edit-{field.Name}", class_ = "editable-row" }
                                        .Data("editable-if", ifAttr.NullOr(a => a.OtherField))
                                        .Data("editable-if-values", ifAttr.NullOr(a => a.Values.Select(v => v.ToString()).JoinString(",")))
                                        .Data("enum-values", field.FieldType.IsEnum && field.FieldType.GetCustomAttribute<FlagsAttribute>() != null ? Enum.GetValues(field.FieldType).Cast<object>().JoinString(",") : null)
                                        ._(new TH(attr.ReadableName), new TD(createTableCellContent(field, attr)));
                                }
                            }
                            static IEnumerable<object> iterateHiddenFields(Type typeToBeEdited)
                            {
                                foreach (var field in typeToBeEdited.GetFields())
                                {
                                    var attr = field.GetCustomAttribute<EditableFieldAttribute>();
                                    // The hidden TwitchPlays field is never serialized, so don't add a hidden input for it.
                                    if (attr != null && attr.ReadableName == null && field.Name != "TwitchPlays")
                                        yield return new INPUT { type = itype.hidden, name = field.Name };
                                }
                            }
                            IEnumerable<object> iterateNestedFields(Type typeToBeEdited)
                            {
                                var nestedFields = typeToBeEdited.GetFields()
                                    .Where(f => f.GetCustomAttribute<EditableNestedAttribute>() != null)
                                    .Select(f => (field: f, attr: f.GetCustomAttribute<EditableFieldAttribute>()))
                                    .Where(tup => tup.attr != null)
                                    .ToArray();
                                yield return new TR(nestedFields.Select(tup => new TH(new H2(new INPUT { type = itype.checkbox, name = tup.field.Name, id = $"nested-{tup.field.Name}" }, "\u00a0", new LABEL { for_ = $"nested-{tup.field.Name}" }._(tup.attr.ReadableName)))));
                                yield return new TR(nestedFields.Select(tup => new TD(
                                    new TABLE { class_ = "fields", id = $"nested-table-{tup.field.Name}" }.Data("nested", tup.field.Name)._(iterateNormalFields(tup.field.FieldType)),
                                    iterateHiddenFields(tup.field.FieldType))));
                            }
                            return Ut.NewArray<object>(
                                new TABLE { class_ = "fields" }._(iterateNormalFields(typeof(KtaneModuleInfo))),
                                iterateHiddenFields(typeof(KtaneModuleInfo)),
                                new TABLE { class_ = "nested" }._(iterateNestedFields(typeof(KtaneModuleInfo))),
                                new DIV { class_ = "submit" }._(new BUTTON { id = "generate-json", type = btype.submit, accesskey = "j" }._("Generate JSON".Accel('J'))));
                        })))),
                    new Func<object>(() => new SCRIPTLiteral($"var translation = {translation.Json};" + _moduleInfoCache.ModuleInfoJs)))));
            resp.UseGzip = UseGzipOption.AlwaysUseGzip;
            return resp;
        }

        private static string cssEscape(string text)
        {
            var sb = new StringBuilder();
            foreach (var ch in text)
                if (ch == '\\' || ch == '\'')
                    sb.Append($"\\{(int) ch:X4} ");
                else
                    sb.Append(ch);
            return sb.ToString();
        }
    }
}
