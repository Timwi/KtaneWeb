using System.Linq;
using RT.Serialization;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    sealed class TranslationInfo
    {
        private static TranslationInfo _default;
        public static TranslationInfo Default => _default ??= makeDefault();

        private static TranslationInfo makeDefault()
        {
            var ti = new TranslationInfo();
            ti.Json = ClassifyJson.Serialize(ti).ToString();
            return ti;
        }

        public string langCode = "en";

        public string title = "Repository of Manual Pages";
        public string titleImg = "HTML/img/repo-logo.svg";
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
        public string joinDiscordAnchor = "Join us on Discord";
        public string faqURL = "More/FAQs.html";
        public string faqAnchor = "Glossary";
        public string tutorialURL = "https://docs.google.com/document/d/1K2la3yW_l6WHXSIaK-b6WKJAsgL9TuGjx8ZhAZeHQko/edit";
        public string tutorialAnchor = "Intro to Playing with Mods";
        public string makingModsAnchor = "Intro to Making Mods";
        public string playlistAnchor = "Tutorial videos playlist";
        public string symbolGuideAnchor = "Repository Symbols Guide";
        public string modIdeaAnchor = "Mod ideas website";
        public string modIdeaPastAnchor = "Mod ideas: spreadsheet of past ideas";
        public string modIdeaSubredditAnchor = "Mod ideas: subreddit";
        public string contentGithubAnchor = "KtaneContent github repository";
        public string contentGithubDesc = "(contains the manuals, Profile Editor, Logfile Analyzer and other static files)";
        public string webGithubAnchor = "KtaneWeb github repository";
        public string webGithubDesc = "(contains this website’s server code)";
        public string lfaAnchor = "Logfile Analyzer";
        public string profileEditorAnchor = "Profile Editor";
        public string downloadProfileAnchor = "Downlad pre-made profiles";
        public string modeEditorAnchor = "Mode Setting Editor";
        public string newModuleAnchor = "Create new module";
        public string downloadPDF = "Download merged PDF for current filter";
        public string ignoredTableAnchor = "Table of ignored modules";
        public string tfcAnchor = "Text Field Calculator";
        public string puzzleAnchor = "PUZZLE";
        public string controlHeader = "Controls to highlight elements in HTML manuals";
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
        public string moduleTypeRegularS = "Regular";
        public string moduleTypeNeedyS = "Needy";
        public string moduleTypeRegular = "Regular module";
        public string moduleTypeNeedy = "Needy module";
        public string moduleTypeWidget = "Widget";
        public string moduleTypeHoldable = "Holdable";
        public string moduleDiffVeryEasy = "very easy";
        public string moduleDiffEasy = "easy";
        public string moduleDiffMedium = "medium";
        public string moduleDiffHard = "hard";
        public string moduleDiffVeryHard = "very hard";
        public string originVanilla = "Vanilla";
        public string originMods = "Mods";
        public string displayMethodList = "List";
        public string displayMethodPeriodic = "Periodic Table";
        public string filterDefuserDifficulty = "Defuser difficulty";
        public string filterExpertDifficulty = "Expert difficulty";
        public string filterType = "Type";
        public string filterOrigin = "Origin";
        public string filterTP = "Twitch Plays";
        public string filterRuleSeed = "Rule seed";
        public string filterSouvenir = "Souvenir";
        public string filterMysteryModule = "Mystery Module";
        public string filterBossStatus = "Boss Status";
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
        public string bossStatusFullBoss = "Full Boss";
        public string bossStatusSemiBoss = "Semi-boss";
        public string bossStatusNotBoss = "Not a boss";
        public string filterSolvesStatus = "Solves Later";
        public string solvesStatusLater = "Solves at end";
        public string solvesStatusNeed = "Needs other solves";
        public string solvesStatusBefore = "Must solve before some";
        public string solvesStatusNA = "N/A";
        public string filterPNeedyStatus = "Pseudo-Needy and Time";
        public string pNeedyStatusPNeedy = "Pseudo-needy";
        public string pNeedyStatusTime = "Heavily time-dependent";
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
        public string[] filterProfileEnabled = { "Enabled by", "" };
        public string[] filterProfileVetoed = { "Vetoed by", "" };
        public string selectableManual = "Manual";
        public string selectableSteam = "Steam Workshop";
        public string selectableSource = "Source code";
        public string selectableTutorial = "Tutorial video";
        public string displayOption = "Display";
        public string displayOriginalName = "Original Name";
        public string displayDescription = "Description";
        public string displayDifficulty = "Difficulty";
        public string displayOrigin = "Origin";
        public string displayTwitch = "Twitch Plays score";
        public string displayTimeMode = "Time Mode score";
        public string displaySouvenir = "Souvenir support";
        public string displayRuleSeed = "Rule seed support";
        public string displayDate = "Date published";
        public string displayID = "Module ID";
        public string displayUpdated = "Last updated";
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
        public string listTutorialVideos = "TutorialVideos";

        [ClassifyIgnore]
        private KtaneFilter[] _filtersCache;
        public KtaneFilter[] Filters => _filtersCache ??= Ut.NewArray(
            KtaneFilter.Slider(filterDefuserDifficulty, "defdiff", mod => mod.DefuserDifficulty, @"mod=>mod.DefuserDifficulty"),
            KtaneFilter.Slider(filterExpertDifficulty, "expdiff", mod => mod.ExpertDifficulty, @"mod=>mod.ExpertDifficulty"),
            KtaneFilter.Checkboxes(filterType, "type", mod => mod.Type, @"mod=>mod.Type"),
            KtaneFilter.Checkboxes(filterOrigin, "origin", mod => mod.Origin, @"mod=>mod.Origin"),
            KtaneFilter.Checkboxes(filterTP, "twitchplays", mod => mod.TwitchPlaysScore == null ? KtaneSupport.NotSupported : KtaneSupport.Supported, @"mod=>mod.TwitchPlays?'Supported':'NotSupported'"),
            KtaneFilter.Checkboxes(filterRuleSeed, "ruleseed", mod => mod.RuleSeedSupport, $@"mod=>mod.RuleSeedSupport||'{KtaneSupport.NotSupported}'"),
            KtaneFilter.Checkboxes(filterSouvenir, "souvenir", mod => mod.Souvenir == null ? KtaneModuleSouvenir.Unexamined : mod.Souvenir.Status, @"mod=>mod.Souvenir?mod.Souvenir.Status:""Unexamined"""),
            KtaneFilter.Checkboxes(filterMysteryModule, "mysterymodule", mod => mod.MysteryModule, $@"mod=>mod.MysteryModule||'{KtaneMysteryModuleCompatibility.NoConflict}'"),
            KtaneFilter.BooleanSet(filterBossStatus, "bossstatus", new[] {
                new KtaneFilterOption { Name = "IsFullBoss", ReadableName = bossStatusFullBoss },
                new KtaneFilterOption { Name = "IsSemiBoss", ReadableName = bossStatusSemiBoss },
                new KtaneFilterOption { Name = "NotABoss", ReadableName = bossStatusNotBoss }},
                mod => mod.IsFullBoss ? "IsFullBoss" : mod.IsSemiBoss ? "IsSemiBoss" : "NotABoss",
                $@"mod=>mod.IsFullBoss?""IsFullBoss"":mod.IsSemiBoss?""IsSemiBoss"":""NotABoss"""),
            KtaneFilter.BooleanSet(filterSolvesStatus, "solveslater", new[] {
                new KtaneFilterOption { Name = "SolvesAtEnd", ReadableName = solvesStatusLater },
                new KtaneFilterOption { Name = "NeedsOtherSolves", ReadableName = solvesStatusNeed },
                new KtaneFilterOption { Name = "SolvesBeforeSome", ReadableName = solvesStatusBefore },
                new KtaneFilterOption { Name = "NotSolvesLater", ReadableName = solvesStatusNA }},
                mod => mod.SolvesAtEnd ? "SolvesAtEnd" : mod.NeedsOtherSolves ? "NeedsOtherSolves" : "NotSolvesLater",
                $@"mod=>mod.SolvesAtEnd?""SolvesAtEnd"":mod.NeedsOtherSolves?""NeedsOtherSolves"":""NotSolvesLater"""),
            KtaneFilter.BooleanSet(filterPNeedyStatus, "pseudoneedytimestatus", new[] {
                new KtaneFilterOption { Name = "IsPseudoNeedy", ReadableName = pNeedyStatusPNeedy },
                new KtaneFilterOption { Name = "IsTimeSensitive", ReadableName = pNeedyStatusTime },
                new KtaneFilterOption { Name = "NotPseudoNeedyOrTime", ReadableName = solvesStatusNA }},
                mod => mod.IsPseudoNeedy ? "IsPseudoNeedy" : mod.IsTimeSensitive ? "IsTimeSensitive" : "NotPseudoNeedyOrTime",
                $@"mod=>mod.IsPseudoNeedy?""IsPseudoNeedy"":mod.IsTimeSensitive?""IsTimeSensitive"":""NotPseudoNeedyOrTime"""));

        [ClassifyIgnore]
        private Selectable[] _selectablesCache;
        public Selectable[] Selectables => _selectablesCache ??= Ut.NewArray(
            new Selectable
            {
                HumanReadable = selectableManual,
                Accel = 'u',
                Icon = "HTML/img/manual.png",
                PropName = "manual",
                UrlFunction = @"mod=>null",
                ShowIconFunction = @"(_,s)=>s.length>0"
            },
            new Selectable
            {
                HumanReadable = selectableSteam,
                Accel = 'W',
                Icon = "HTML/img/steam-workshop-item.png",
                PropName = "steam",
                UrlFunction = @"mod=>mod.SteamID?`http://steamcommunity.com/sharedfiles/filedetails/?id=${mod.SteamID}`:null",
                ShowIconFunction = @"mod=>!!mod.SteamID",
            },
            new Selectable
            {
                HumanReadable = selectableSource,
                Accel = 'c',
                Icon = "HTML/img/unity.png",
                PropName = "source",
                UrlFunction = @"mod=>mod.SourceUrl",
                ShowIconFunction = @"mod=>!!mod.SourceUrl",
            },
            new Selectable
            {
                HumanReadable = selectableTutorial,
                Accel = 'T',
                Icon = "HTML/img/video.png",
                PropName = "video",
                UrlFunction = @"mod=>mod.TutorialVideoUrl && (mod.TutorialVideoUrl[languageCodesReverse[translation.langCode]] || mod.TutorialVideoUrl.default)",
                ShowIconFunction = @"mod=>mod.TutorialVideoUrl && (!!mod.TutorialVideoUrl[languageCodesReverse[translation.langCode]] || !!mod.TutorialVideoUrl.default)",
            });

        [ClassifyIgnore]
        private (string readable, string id)[] _displaysCache;
        public (string readable, string id)[] Displays => _displaysCache ??= Ut.NewArray<(string readable, string id)?>(
            langCode == "en" ? null : (readable: displayOriginalName, id: "name"),
            (readable: displayDescription, id: "description"),
            (readable: displayDifficulty, id: "difficulty"),
            (readable: displayOrigin, id: "origin"),
            (readable: displayTwitch, id: "twitch"),
            (readable: displayTimeMode, id: "time-mode"),
            (readable: displaySouvenir, id: "souvenir"),
            (readable: displayRuleSeed, id: "rule-seed"),
            (readable: displayDate, id: "published"),
            (readable: displayID, id: "id"),
            (readable: displayUpdated, id: "last-updated")).WhereNotNull().ToArray();

        [ClassifyIgnore]
        public string Json;
    }
}
