using System.Collections.Generic;
using System.Linq;
using RT.Serialization;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    sealed class TranslationInfo
    {
        public static readonly Dictionary<string, string> LanguageCodeToName = new()
        {
            ["ca-CT"] = "Català",
            ["da"] = "Dansk",
            ["de"] = "Deutsch",
            ["et"] = "Eesti",
            ["en"] = "English",
            ["eu"] = "Euskara",
            ["es"] = "Español",
            ["eo"] = "Esperanto",
            ["fr"] = "Français",
            ["fy"] = "Frysk",
            ["it"] = "Italiano",
            ["hu"] = "Magyar",
            ["nl"] = "Nederlands",
            ["no"] = "Norsk",
            ["pl"] = "Polski",
            ["pt-PT"] = "Português",
            ["pt-BR"] = "Português do Brasil",
            ["fi"] = "Suomi",
            ["sv"] = "Svenska",
            ["tr"] = "Türkçe",
            ["ca-VA"] = "Valencià",
            ["cs"] = "Čeština",
            ["el"] = "Ελληνικά",
            ["bg"] = "Български",
            ["ru"] = "Русский",
            ["uk"] = "Українська",
            ["he"] = "עברית",
            ["ar"] = "العربية",
            ["th"] = "ภาษาไทย",
            ["ja"] = "日本語",
            ["zh-CN"] = "简体中文",
            ["zh-TW"] = "繁體中文",
            ["ko"] = "한국어"
        };

        private static TranslationInfo _default;
        public static TranslationInfo Default => _default ??= makeDefault();

        private static TranslationInfo makeDefault()
        {
            var ti = new TranslationInfo();
            ti.Json = ClassifyJson.Serialize(ti).ToString();
            return ti;
        }

        public string langCode = "en";

        public string numberSystemJs = "n => n === 1 ? 0 : 1";

        public string title = "Repository of Manual Pages";
        public string titleImg = "HTML/img/repo-logo.svg";
        public string langSelectorLabel = "Language";
        public string searchFind = "Find:";
        public string searchMission = "Mission:";
        public string searchNames = "Names";
        public string searchAuthors = "Authors";
        public string searchDescriptions = "Descriptions";
        public string popupLinks = "Links";
        public string popupTools = "Tools";
        public string popupView = "View";
        public string popupMore = "More";
        public string tabRuleSeed = "Rule seed";
        public string tabFilters = "Filters";
        public string tabOptions = "Options";
        public string columnLinks = "Links";
        public string columnName = "Name";
        public string columnInformation = "Information";
        public string by = "by";
        public string more = "MORE";
        public string tags = "Tags: ";
        public string joinDiscordAnchor = "Join us on Discord";
        public string glossaryURL = "More/Glossary.html";
        public string glossaryAnchor = "Glossary";
        public string tutorialURL = "https://docs.google.com/document/d/1K2la3yW_l6WHXSIaK-b6WKJAsgL9TuGjx8ZhAZeHQko/edit";
        public string tutorialAnchor = "Intro to Playing with Mods";
        public string makingModsAnchor = "Intro to Making Mods";
        public string playlistAnchor = "Tutorial videos playlist";
        public string symbolGuideAnchor = "Repository Symbols Guide";
        public string challengeBombAnchor = "Challenge Bombs Site";
        public string modIdeaAnchor = "Mod ideas website";
        public string modIdeaPastAnchor = "Mod ideas: spreadsheet of past ideas";
        public string modIdeaSubredditAnchor = "Mod ideas: subreddit";
        public string contentGithubAnchor = "KtaneContent GitHub repository";
        public string contentGithubDesc = "(contains the manuals, Profile Editor, Logfile Analyzer and other static files)";
        public string webGithubAnchor = "KtaneWeb GitHub repository";
        public string webGithubDesc = "(contains this website’s server code)";
        public string lfaAnchor = "Logfile Analyzer";
        public string profileEditorAnchor = "Profile Editor";
        public string downloadProfileAnchor = "Download pre-made profiles";
        public string modeEditorAnchor = "Mode Setting Editor";
        public string newModuleAnchor = "Create new module";
        public string downloadPDF = "Download merged PDF for current filter";
        public string ignoredTableAnchor = "Table of ignored modules";
        public string ignoreTableURL = "More/Ignore%20Table.html";
        public string tfcAnchor = "Text Field Calculator";
        public string puzzleAnchor = "PUZZLES";
        public string quizAnchor = "QUIZZES";
        public string quizURL = "More/Quiz.html";
        public string controlHeader = "Controls for highlighting elements in HTML manuals";
        public string[][] controls = {
            new string[] { "Highlight a table column", "Ctrl+Click (Windows)", "Command+Click (Mac)" },
            new string[] { "Highlight a table row", "Shift+Click" },
            new string[] { "Highlight a table cell or an item in a list", "Alt+Click (Windows)", "Ctrl+Shift+Click (Windows)", "Command+Shift+Click (Mac)" },
            new string[] { "Change highlighter color", "Alt+0 through Alt+9 (digits)" },
            new string[] { "Additional options", "Alt+O (letter)" }
        };
        public string fileLocationHeader = "Default file locations";
        public string fileLocationGame = "Game";
        public string fileLocationLogfile = "Logfile";
        public string fileLocationProfile = "Mod Selector Profiles";
        public string fileLocationSetting = "Mod Settings";
        public string fileLocationScreenshot = "Screenshots";
        public string expertTemplateAnchor = "Experting template";
        public string expertTemplateDesc = "(printable page with boxes to fill in while experting)";
        public string templateManualAnchor = "Template manual";
        public string templateManualDesc = "(for modders wishing to create a manual page for a new module)";
        public string demilAnchor = "DeMiL Mission Viewer";
        public string demilDesc = "(see the missions you have installed, see their details, or start a mission)";
        public string originVanilla = "Vanilla";
        public string originMods = "Mods";
        public string moduleTypeRegularS = "Regular";
        public string moduleTypeNeedyS = "Needy";
        public string moduleTypeRegular = "Regular module";
        public string moduleTypeNeedy = "Needy module";
        public string moduleTypeWidget = "Widget";
        public string moduleTypeHoldable = "Holdable";
        public string moduleDiffTrivial = "trivial";
        public string moduleDiffVeryEasy = "very easy";
        public string moduleDiffEasy = "easy";
        public string moduleDiffMedium = "medium";
        public string moduleDiffHard = "hard";
        public string moduleDiffVeryHard = "very hard";
        public string moduleDiffExtreme = "extreme";
        public string compatibilityCompatible = "Compatible";
        public string compatibilityProblematic = "Problematic";
        public string compatibilityUnplayable = "Unplayable";
        public string displayMethodList = "List";
        public string displayMethodPeriodic = "Periodic Table";
        public string filterDefuserDifficulty = "Defuser difficulty";
        public string filterExpertDifficulty = "Expert difficulty";
        public string filterType = "Type";
        public string filterOrigin = "Origin";
        public string filterCompatibility = "Compatibility";
        public string filterTP = "Twitch Plays";
        public string filterRuleSeed = "Rule seed";
        public string filterSouvenir = "Souvenir";
        public string filterMysteryModule = "Mystery Module";
        public string filterBossStatus = "Boss Status";
        public string filterHasTutorial = "Has Tutorial";
        public string filterTutorial = "Tutorial";
        public string filterNotSupported = "Not supported";
        public string filterSupported = "Supported";
        public string filterUnexamined = "Unexamined";
        public string filterNotCandidate = "Not a candidate";
        public string filterConsidered = "Considered";
        public string filterMMNoConfilct = "No conflict";
        public string filterMMNotHide = "MM must not hide this";
        public string filterMMNotRequire = "MM must not require this";
        public string filterMMNotUse = "MM must not use this at all";
        public string filterMMAutoSovle = "MM must auto-solve";
        public string licenseOpenSource = "The module has its source code released and will follow the module’s license.";
        public string licenseOpenSourceClone = "The module's source code is a clone/fork of its original repository, and will follow the license of the original.";
        public string licenseRepublishable = "The module may be republished on someone else’s Steam account. Any work may not be reused.";
        public string licenseRestricted = "The module may not be republished and any work may not be reused.";
        public string bossStatusFullBoss = "Full Boss";
        public string bossStatusSemiBoss = "Semi-boss";
        public string bossStatusNotBoss = "Not a boss";

        public string filterQuirks = "Quirks";
        public string quirkNone = "None";
        public string quirkSolvesLater = "Solves at end";
        public string quirkSolvesLaterExplain = "The module is only solvable after all non-ignored modules are solved (at the end of the bomb). In general, bosses have this quirk and all bosses should ignore modules with this quirk.";
        public string quirkNeedsSolves = "Needs other solves";
        public string quirkNeedsSolvesExplain = "The module cannot be solved until some, but not necessarily all, other non-ignored regular modules are solved first. In general, semi-bosses ignore modules with this quirk and often have this quirk.";
        public string quirkSolvesBefore = "Must solve before some";
        public string quirkSolvesBeforeExplain = "The module must be solved before some other non-ignored modules. In general, all modules with this quirk should ignore each other.";
        public string quirkWillSolveSuddenly = "Will solve suddenly";
        public string quirkWillSolveSuddenlyExplain = "The module will suddenly present a small window of time in which it will solve even if the solution is incorrect or if strikes are generated.";
        public string quirkSolvesWithOthers = "May solve with others";
        public string quirkSolvesWithOthersExplain = "The module may disarm itself immediately in response to another module being solved.";
        public string quirkPseudoNeedy = "Pseudo-needy";
        public string quirkPseudoNeedyExplain = "The module poses a recurring hazard in a similar fashion to a needy before it can be solved.";
        public string quirkTimeDependent = "Heavily time-dependent";
        public string quirkTimeDependentExplain = "The module has very precise timing requirements or can only be solved at an exact time.";
        public string quirkNeedsImmediateAttention = "Needs immediate attention";
        public string quirkNeedsImmediateAttentionExplain = "The module must be solved (or is significantly easier to solve) within a short window of time at bomb start.";
        public string quirkInstantDeath = "Instant death";
        public string quirkInstantDeathExplain = "Failing to solve or striking on the module ends the bomb, often the bomb will immediately detonate or the whole game will exit.";

        public string timeModeUnassigned = "This module does not have any assigned Time Mode score.";
        public string timeModeFromTP = "This module uses its Twitch Plays score as its Time Mode score.";
        public string timeModeCommunityScore = "This module has a community-assigned Time Mode score.";
        public string timeModeAssigned = "This module has an assigned Time Mode score.";

        public string flagYes = "Yes";
        public string flagNo = "No";
        public string flagEither = "Either";
        public string sortOrderHeader = "Sort order";
        public string sortOrderName = "Sort by name";
        public string sortOrderDefDifficulty = "Sort by defuser difficulty";
        public string sortOrderExpDifficulty = "Sort by expert difficulty";
        public string sortOrderTP = "Sort by score on TP:KTANE";
        public string sortOrderTime = "Sort by score in Time Mode";
        public string sortOrderDate = "Sort by date published";
        public string sortOrderReverse = "Reverse";
        public string filterProfile = "Profile";
        public string filterProfileOpen = "Filter by a profile";
        public string[] filterProfileEnabled = { "Enabled by ", "" };
        public string[] filterProfileVetoed = { "Vetoed by ", "" };
        public string selectableManual = "Manual";
        public string selectableSteam = "Steam Workshop";
        public string selectableSource = "Source code";
        public string selectableSourceClone = "Source code (clone)";
        public string selectableTutorial = "Tutorial videos";
        public string displayOption = "Display";
        public string displayOriginalName = "Original Name";
        public string displayDescription = "Description";
        public string displayTags = "Tags";
        public string displayDifficulty = "Difficulty";
        public string displayOrigin = "Origin";
        public string displayTwitch = "Twitch Plays score";
        public string displayTimeMode = "Time Mode score";
        public string displaySouvenir = "Souvenir support";
        public string displaySymbol = "PT Symbol";
        public string displayQuirks = "Quirks";
        public string displayRuleSeed = "Rule seed support";
        public string displayDate = "Date published";
        public string displayID = "Module ID";
        public string displayUpdated = "Last updated";
        public string displayAllContributors = "All contributors";
        public string displayRestrictedManuals = "Restricted manuals";
        public string searchOption = "Search options";
        public string searchSteamID = "Search by Steam ID";
        public string searchSymbol = "Search by Symbol";
        public string searchModuleID = "Search by Module ID";
        public string findBarOption = "When using the Find bar";
        public string findBarMatch = "Show matches only";
        public string findBarMatchLimit = "Limit:";
        public string findBarScroll = "Scroll first match into view";
        public string themeOption = "Site theme";
        public string themeLight = "Light";
        public string themeDark = "Dark";
        public string linkOption = "Make links go to";
        public string languagesOption = "Languages";
        public string languagesToggle = "Toggle All Languages";
        public string listTutorialVideos = "Tutorial Videos";
        public string contactInformation = "Contact Information";
        public string selectPreferredManual = "Select your preferred manual for this module.";
        public string lastUpdated = "(Last updated)";
        public string findExampleLogfile = "Find example logfile";
        public string editModule = "Edit this module";
        public string editWidget = "Edit this widget";
        public string editHoldable = "Edit this holdable";

        public string ruleSeedLabel = "Rule seed:";
        public string ruleSeedExplanation = "Varies the rules/manuals for supported modules.";
        public string ruleSeedLink = "Requires the {Rule Seed Modifier} mod.";
        public string ruleSeedDefault = "Set to 1 to revert to default rules.";

        public string bottomLineAll = "{0}.";
        public string bottomLineSome = "{0}; {1}. {2}";
        public string bottomLineShowAllLink = "Show all";
        public string[] bottomLineItems = new[] { "1 item", "{0} items" };
        public string[] bottomLineShowingFirst = new[] { "showing first only", "showing first {0}" };

        public string missionsLoading = "Loading...";
        public string missionsNoneSelected = "(no mission selected)";

        public string souvenirUnexamined = "We have not yet decided whether this module is a candidate for inclusion in Souvenir.";
        public string souvenirNotACandidate = "This module is not a candidate for inclusion in Souvenir.";
        public string souvenirConsidered = "This module may be a candidate for inclusion in Souvenir.";
        public string souvenirSupported = "This module is included in Souvenir. Refer to the Souvenir manual for details.";

        public string tpScore = "This module can be played in “Twitch Plays: KTANE” for {0}.";
        public string tpScoreTbd = "a point score yet to be determined";
        public string[] tpScoreBase = new[] { "1 base point", "{0} base points" };
        public string[] tpScoreTime = new[] { "1 point per second", "{0} points per second" };
        public string[] tpScoreNeedy = new[] { "1 point per deactivation", "{0} points per deactivation" };
        public string[] tpScoreAction = new[] { "1 point per action", "{0} points per action" };
        public string[] tpScoreSolve = new[] { "1 point per module", "{0} points per module" };

        [ClassifyIgnore]
        private KtaneFilter[] _filtersCache1;
        [ClassifyIgnore]
        private KtaneFilter[] _filtersCache2;
        public KtaneFilter[] Filters1 => _filtersCache1 ??= Ut.NewArray(
            KtaneFilter.Slider(filterDefuserDifficulty, "defdiff", mod => mod.DefuserDifficulty, @"mod=>mod.DefuserDifficulty"),
            KtaneFilter.Slider(filterExpertDifficulty, "expdiff", mod => mod.ExpertDifficulty, @"mod=>mod.ExpertDifficulty"),
            KtaneFilter.Checkboxes(filterType, "type", mod => mod.Type, @"mod=>mod.Type"),
            KtaneFilter.Checkboxes(filterOrigin, "origin", mod => mod.Origin, @"mod=>mod.Origin"),
            KtaneFilter.Checkboxes(filterCompatibility, "compatibility", mod => mod.Compatibility, @"mod=>mod.Compatibility"),
            KtaneFilter.Checkboxes(filterTP, "twitchplays", mod => mod.TwitchPlaysScore == null ? KtaneSupport.NotSupported : KtaneSupport.Supported, @"mod=>mod.TwitchPlays?'Supported':'NotSupported'"),
            KtaneFilter.Checkboxes(filterRuleSeed, "ruleseed", mod => mod.RuleSeedSupport, $@"mod=>mod.RuleSeedSupport||'{KtaneSupport.NotSupported}'"),
            KtaneFilter.Checkboxes(filterSouvenir, "souvenir", mod => mod.Souvenir == null ? KtaneModuleSouvenir.Unexamined : mod.Souvenir.Status, @"mod=>mod.Souvenir?mod.Souvenir.Status:""Unexamined"""));
        public KtaneFilter[] Filters2 => _filtersCache2 ??= Ut.NewArray(
            KtaneFilter.Checkboxes(filterMysteryModule, "mysterymodule", mod => mod.MysteryModule, $@"mod=>mod.MysteryModule||'{KtaneMysteryModuleCompatibility.NoConflict}'"),
            KtaneFilter.Checkboxes(filterBossStatus, "bossstatus", mod => mod.BossStatus, $@"mod=>mod.BossStatus||'{KtaneBossStatus.NotABoss}'"),
            KtaneFilter.Checkboxes(filterTutorial, "hastutorial", mod => mod.TutorialVideos == null ? KtaneTutorialStatus.NoTutorial : KtaneTutorialStatus.HasTutorial, $@"mod=>mod.TutorialVideos?'HasTutorial':'NoTutorial'"),
            KtaneFilter.Flags(filterQuirks, "quirks", mod => mod.Quirks, $@"mod=>mod.Quirks||''"));

        [ClassifyIgnore]
        private Selectable[] _selectablesCache;
        public Selectable[] Selectables => _selectablesCache ??= Ut.NewArray(
            new Selectable
            {
                Accel = 'u',
                PropName = "manual",
                HumanReadable = selectableManual,
                IconFunction = @"mod=>'HTML/img/manual.png'",
                UrlFunction = @"mod=>mod.ManualIconUrl",
                ShowIconFunction = @"(_,s)=>s.length>0"
            },
            new Selectable
            {
                Accel = 'W',
                PropName = "steam",
                HumanReadable = selectableSteam,
                IconFunction = @"mod=>'HTML/img/steam-workshop-item.png'",
                UrlFunction = @"mod=>mod.SteamID?`http://steamcommunity.com/sharedfiles/filedetails/?id=${mod.SteamID}`:null",
                ShowIconFunction = @"mod=>!!mod.SteamID",
            },
            new Selectable
            {
                Accel = 'c',
                PropName = "source",
                HumanReadable = selectableSource,
                HumanReadableFunction = @$"mod=>mod.License==='OpenSourceClone'?'{selectableSourceClone}':'{selectableSource}'",
                IconFunction = @"mod=>mod.License==='OpenSourceClone'?'HTML/img/unity-clone.png':'HTML/img/unity.png'",
                UrlFunction = @"mod=>mod.SourceUrl",
                ShowIconFunction = @"mod=>!!mod.SourceUrl",
            },
            new Selectable
            {
                Accel = 'T',
                PropName = "video",
                HumanReadable = selectableTutorial,
                IconFunction = @"mod=>'HTML/img/video.png'",
                UrlFunction = @"mod=>mod.TutorialVideos&&mod.TutorialVideos[0].Url",
                ShowIconFunction = @"mod=>!!mod.TutorialVideos&&mod.TutorialVideos.length>0",
            });

        [ClassifyIgnore]
        private (string readable, string id)[] _displaysCache;
        public (string readable, string id)[] Displays => _displaysCache ??= Ut.NewArray<(string readable, string id)?>(
            langCode == "en" ? null : (readable: displayOriginalName, id: "origname"),
            (readable: displayDescription, id: "description"),
            (readable: displayTags, id: "tags"),
            (readable: displayDifficulty, id: "difficulty"),
            (readable: displayOrigin, id: "origin"),
            (readable: displayTwitch, id: "twitch"),
            (readable: displayTimeMode, id: "time-mode"),
            (readable: displaySouvenir, id: "souvenir"),
            (readable: displaySymbol, id: "symbol"),
            (readable: displayQuirks, id: "quirks"),
            (readable: displayRuleSeed, id: "rule-seed"),
            (readable: displayDate, id: "published"),
            (readable: displayID, id: "id"),
            (readable: displayUpdated, id: "last-updated"),
            (readable: displayAllContributors, id: "all-contributors"),
            (readable: displayRestrictedManuals, id: "restricted-manuals")
        ).WhereNotNull().ToArray();

        [ClassifyIgnore]
        public string Json;
    }
}
