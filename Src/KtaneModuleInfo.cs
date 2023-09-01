using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Json;
using RT.Serialization;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

    sealed class KtaneModuleInfo : IEquatable<KtaneModuleInfo>, IComparable<KtaneModuleInfo>, IClassifyObjectProcessor, IClassifyJsonObjectProcessor
    {
        [EditableField("Type", "Regular module = solvable; Widget = edgework item.")]
        public KtaneModuleType Type = KtaneModuleType.Regular;
        [EditableField(null)]   // invisible field
        public KtaneModuleOrigin Origin = KtaneModuleOrigin.Mods;

        [ClassifyIgnore]
        public string FileName;

        [EditableField("Name", "The display name of the module or widget.")]
        public string Name;
        [ClassifyIgnoreIfDefault]
        public string DisplayName;
        [ClassifyNotNull, EditableField("Descriptions", "A concise description of what sets this module or widget apart from others. For Tags, provide a set of keywords to find a module based on its appearance (e.g. blue-background, 12-buttons, etc.).")]
        public DescriptionInfo[] Descriptions = new[] { new DescriptionInfo { Language = "English", Description = "" } };
        [EditableField("Module ID", "The ID that mission makers need for this module. This is the same as the ModuleType property on the KMBombModule component.")]
        public string ModuleID;
        [EditableField("Sort key", "The name of the module or widget in all-caps, without spaces, and without initial “The”.")]
        public string SortKey;
        [EditableField("Steam ID", "The numerical ID of the Steam Workshop item.")]
        public string SteamID;
        [ClassifyIgnoreIfDefault, EditableField("Author", "A comma-separated list of contributors to the development of the module or widget.")]
        public string Author;

        [ClassifyIgnoreIfDefault, EditableNested, EditableField("Contributors", "The roles of the contributors to the development the module or widget. The author field will be automatically generated from this if it's empty.")]
        public ContributorInfo Contributors;

        [ClassifyIgnoreIfDefault, EditableField("Source code", "A link to the source code of the module or widget, usually a link to a GitHub repository.")]
        public string SourceUrl;
        [ClassifyIgnoreIf(KtaneModuleLicense.Restricted), EditableField("License", "Specifies how the module is licensed. Specifically, what can be reused and republished.")]
        public KtaneModuleLicense License = KtaneModuleLicense.Restricted;
        [ClassifyIgnoreIfDefault, EditableField("Tutorial videos", "Links to tutorial videos, if available. Sites other than YouTube are of course totally fine. Specify the language in the language itself (e.g. Français instead of French). Description is optional; only fill that in to describe the differences between multiple tutorial videos in the same language.")]
        public TutorialVideoInfo[] TutorialVideos = null;
        [ClassifyIgnoreIfDefault, EditableField("Symbol", "A symbol for the Periodic Table of Modules. Only the first letter will be capitalized."), EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy, KtaneModuleType.Holdable)]
        public string Symbol;

        [ClassifyIgnoreIfDefault, ClassifyIgnoreIfEmpty, EditableField("Obsolete Steam IDs", "Numerical IDs of Steam Workshop items containing old versions of this mod that have since been reuploaded.", AllowedSeparators = new[] { ';', ',' })]
        public string[] ObsoleteSteamIDs;

        [ClassifyIgnoreIfDefault, ClassifyIgnoreIfEmpty, EditableField("Ignored by DBML", "Specify if DBML should not be used to load this module.")]
        public bool DBMLIgnored;

        [EditableField("Compatibility", "Specify if the module or widget has any known issues.\nUse “Problematic” if the issues are cosmetic.\nUse “Unplayable” if a bug causes undeserved strikes or softlocked games, even if rare.")]
        public KtaneModuleCompatibility Compatibility = KtaneModuleCompatibility.Compatible;
        [ClassifyIgnoreIfDefault, EditableField("Explain", "Explain the Compatibility setting above."), EditableIf(nameof(Compatibility), KtaneModuleCompatibility.Problematic, KtaneModuleCompatibility.Unplayable)]
        public string CompatibilityExplanation = null;
        [EditableField("Published", "The date of publication.")]
        public DateTime Published = DateTime.UtcNow.Date;

        // The following are only relevant for modules (not widgets)
        [ClassifyIgnoreIfDefault, EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy, KtaneModuleType.Holdable)]
        [EditableField("Defuser difficulty", "An approximate difficulty rating for the defuser.")]
        public KtaneModuleDifficulty? DefuserDifficulty;
        [ClassifyIgnoreIfDefault, EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy, KtaneModuleType.Holdable)]
        [EditableField("Expert difficulty", "An approximate difficulty rating for the expert.")]
        public KtaneModuleDifficulty? ExpertDifficulty;

        [ClassifyIgnoreIfDefault, EditableField("Rule-seed", "Does the module vary its rules and manual under the Rule Seed Modifier?")]
        public KtaneSupport RuleSeedSupport = KtaneSupport.NotSupported;

        [ClassifyIgnoreIfDefault, EditableField("Boss status", "Does this module have interaction or new information given after every solve of a non-ignored regular module? Use “semi-boss” if it does not require all such modules to be solved. Use “full boss” if this module can only be solved at the end of the bomb.")]
        public KtaneBossStatus BossStatus = KtaneBossStatus.NotABoss;

        [ClassifyIgnoreIfDefault, EditableField("Quirks", "Does this module impact the overall bomb defusal process in special ways?")]
        public KtaneQuirk Quirks = 0;

        // Specifies which modules this module should ignore. Applies to boss and semi-boss modules such as Forget Me Not, Encryption Bingo, Hogwarts, etc.
        [ClassifyIgnoreIfDefault, ClassifyIgnoreIfEmpty, EditableField("Ignore list", "Use only for boss modules. Specify which other modules this module should ignore (semicolon-separated list). Use “+SolvesAtEnd”, “+NeedsOtherSolves”, “+SolvesBeforeSome”, “+WillSolveSuddenly”, “+SolvesWithOthers”, or “+TimeDependent” to include all modules marked as such. Prepend a module name with a minus (“-”) to exclude it.")]
        public string[] Ignore = null;

        [ClassifyIgnoreIfDefault, EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy, KtaneModuleType.Holdable)]
        [EditableField("Translation of", "Only enter this if this module is a translation of another module. Specify the original name of the other module (e.g., “The Button”). It will not be listed separately on the website.")]
        public string TranslationOf = null;

        [ClassifyIgnoreIfDefault, EditableNested, EditableField("Souvenir", "Uncheck for modules that have not been assessed."), EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        public KtaneSouvenirInfo Souvenir = null;

        [ClassifyIgnoreIfDefault, EditableField("Mystery Module compatibility", "Specify how Mystery Module may affect this module. Use “MM must not hide this” if this module requires other modules to be solved earlier (e.g. Encryption Bingo, Hogwarts). Use “MM must not require this” if the module depends on whether or not other modules are on the bomb (e.g. Free Parking, Mafia). Use “MM must not use this at all” if both is the case. Use “MM must auto-solve” if this module imposes a solve order on other modules (e.g. Organization, Turn the Keys).")]
        public KtaneMysteryModuleCompatibility MysteryModule = KtaneMysteryModuleCompatibility.NoConflict;

        [ClassifyIgnoreIfDefault]
        public int? PageRenderTime = null;

        // This information is imported from a spreadsheet, so not serialized in JSON.
        [ClassifyIgnore]
        public decimal? TwitchPlaysScore = null;

        // This information is imported from a spreadsheet, so not serialized in JSON.
        [ClassifyIgnore]
        public KtaneTimeModeInfo TimeMode = null;

        public object Icon(KtaneWebConfig config) => Path.Combine(config.BaseDir, "Icons", Name + ".png")
            .Apply(f => new IMG { class_ = "mod-icon", alt = Name, title = Name, src = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(File.Exists(f) ? f : Path.Combine(config.BaseDir, "Icons", "blank.png")))}" });

        public bool Equals(KtaneModuleInfo other)
        {
            return other != null &&
                other.Author == Author &&
                other.Compatibility == Compatibility &&
                other.DefuserDifficulty == DefuserDifficulty &&
                other.ExpertDifficulty == ExpertDifficulty &&
                other.ModuleID == ModuleID &&
                other.Name == Name &&
                other.Origin == Origin &&
                other.Published == Published &&
                other.Published.Kind == Published.Kind &&
                other.RuleSeedSupport == RuleSeedSupport &&
                other.SortKey == SortKey &&
                other.SourceUrl == SourceUrl &&
                Equals(other.Souvenir, Souvenir) &&
                other.SteamID == SteamID &&
                other.Symbol == Symbol &&
                (other.Descriptions == null || other.Descriptions.Length == 0 ? Descriptions == null || Descriptions.Length == 0 : other.Descriptions.SequenceEqual(Descriptions)) &&
                (other.TutorialVideos == null || other.TutorialVideos.Length == 0 ? TutorialVideos == null || TutorialVideos.Length == 0 : other.TutorialVideos.SequenceEqual(TutorialVideos)) &&
                other.Type == Type;
        }

        public override int GetHashCode() => Ut.ArrayHash(Author, Compatibility, DefuserDifficulty, Descriptions, ExpertDifficulty, Name, Origin, Published, RuleSeedSupport, SortKey, SourceUrl, Souvenir, SteamID, Symbol, TutorialVideos, Type);
        public override bool Equals(object obj) => Equals(obj as KtaneModuleInfo);
        public override string ToString() => Name;

        int IComparable<KtaneModuleInfo>.CompareTo(KtaneModuleInfo other) => other == null ? 1 : SortKey == null ? (other.SortKey == null ? 0 : -1) : other.SortKey == null ? 1 : SortKey.CompareTo(other.SortKey);

        void IClassifyObjectProcessor<JsonValue>.AfterDeserialize(JsonValue element)
        {
            if (SortKey == null || SortKey == "")
                SortKey = Regex.Replace(Name, @"^The |[^a-zA-Z0-9]", "", RegexOptions.IgnoreCase).ToUpperInvariant();

            if (Type == KtaneModuleType.Regular || Type == KtaneModuleType.Needy || Type == KtaneModuleType.Holdable)
            {
                DefuserDifficulty ??= KtaneModuleDifficulty.Easy;
                ExpertDifficulty ??= KtaneModuleDifficulty.Easy;
            }
            else
            {
                DefuserDifficulty = null;
                ExpertDifficulty = null;
                RuleSeedSupport = KtaneSupport.NotSupported;
            }

            if (TutorialVideos != null && TutorialVideos.Length == 0)
                TutorialVideos = null;

            if (Type != KtaneModuleType.Regular)
                Souvenir = new KtaneSouvenirInfo { Status = KtaneModuleSouvenir.NotACandidate };
            else if (Souvenir != null && Souvenir.Status == KtaneModuleSouvenir.Unexamined)
                Souvenir = null;
            else if (Souvenir != null && Souvenir.Status != KtaneModuleSouvenir.Considered)
                Souvenir.Explanation = null;

            if (Ignore != null && Ignore.Length == 0)
                Ignore = null;

            if (Symbol != null && Symbol.Length > 0)
                Symbol = Symbol.Substring(0, 1).ToUpperInvariant() + Symbol.Substring(1).ToLowerInvariant();

            if (SourceUrl != null && License != KtaneModuleLicense.OpenSourceClone)
                License = KtaneModuleLicense.OpenSource;

            if (element.ContainsKey("Description") && element["Description"].GetStringSafe() is string descr && !string.IsNullOrWhiteSpace(descr))
            {
                var p = descr.IndexOf(" Tags: ");
                if (p == -1)
                    Descriptions = new[] { new DescriptionInfo { Language = "English", Description = descr } };
                else
                    Descriptions = new[] { new DescriptionInfo { Language = "English", Description = descr.Substring(0, p), Tags = descr.Substring(p + " Tags: ".Length) } };
            }
        }

        void IClassifyObjectProcessor<JsonValue>.AfterSerialize(JsonValue element)
        {
            if (element is JsonDict && element.ContainsKey("Published") && element["Published"].GetStringSafe()?.EndsWith("Z") == true)
                element["Published"] = element["Published"].GetString().Apply(s => s.Remove(s.Length - 1));
            if (Type != KtaneModuleType.Regular && element is JsonDict && element.ContainsKey("Souvenir"))
                element.Remove("Souvenir");

            if (Descriptions.Select(d => d.Language).SequenceEqual("English"))
            {
                element["Description"] = string.IsNullOrWhiteSpace(Descriptions[0].Tags) ? Descriptions[0].Description : $"{Descriptions[0].Description} Tags: {Descriptions[0].Tags}";
                element.Remove("Descriptions");
            }
        }

        void IClassifyObjectProcessor.BeforeSerialize()
        {
            // This is a bit hacky, but let’s set License to its default value (which happens to be Restricted) to make the serializer omit that field.
            // AfterDeserialize() will set it back to OpenSource if SourceUrl != null.
            if (License == KtaneModuleLicense.OpenSource && SourceUrl != null)
                License = KtaneModuleLicense.Restricted;
        }

        void IClassifyObjectProcessor.AfterDeserialize() { }
        void IClassifyObjectProcessor<JsonValue>.BeforeSerialize() { }
        void IClassifyObjectProcessor<JsonValue>.BeforeDeserialize(JsonValue element) { }
    }

    sealed class KtaneSouvenirInfo : IEquatable<KtaneSouvenirInfo>
    {
        [EditableField("Status", "Status of Souvenir implementation.")]
        public KtaneModuleSouvenir Status;
        [ClassifyIgnoreIfDefault, EditableIf(nameof(Status), KtaneModuleSouvenir.Considered), EditableField("Explain", "Explain what question(s) Souvenir could ask about this module.", Multiline = true)]
        public string Explanation;

        public static object GetTag(KtaneSouvenirInfo inf)
        {
            var value = inf == null ? KtaneModuleSouvenir.NotACandidate : inf.Status;
            var attr = value.GetCustomAttribute<KtaneSouvenirInfoAttribute>();
            return new DIV
            {
                class_ = "inf-souvenir" + (inf == null || inf.Explanation == null ? null : " souvenir-explanation"),
                title = attr.Tooltip + (inf == null || inf.Explanation == null ? null : "\n" + inf.Explanation)
            }._(attr.Char);
        }

        public override bool Equals(object obj) => obj != null && obj is KtaneSouvenirInfo info && Equals(info);
        public bool Equals(KtaneSouvenirInfo other) => other != null && other.Status == Status && other.Explanation == Explanation;
        public override int GetHashCode() => Ut.ArrayHash(Status, Explanation);
    }

    sealed class KtaneTimeModeInfo : IEquatable<KtaneTimeModeInfo>
    {
        [ClassifyIgnoreIfDefault, EditableIf(nameof(KtaneModuleInfo.Type), KtaneModuleType.Regular), EditableField("Score", "For regular modules, the score for solving it. For needy modules, depends on the scoring method.")]
        public decimal? Score;
        [ClassifyIgnoreIfDefault, EditableIf(nameof(KtaneModuleInfo.Type), KtaneModuleType.Regular), EditableField("Score per module", "For boss modules, a score value that is multiplied by the total number of modules on the bomb.")]
        public decimal? ScorePerModule;          // for boss modules like FMN
        [ClassifyIgnoreIfDefault, EditableIf(nameof(KtaneModuleInfo.Type), KtaneModuleType.Regular), EditableField("Score origin", "The origin of this module's Time Mode score.")]
        public KtaneTimeModeOrigin? Origin;

        public override bool Equals(object obj) => obj != null && obj is KtaneTimeModeInfo info && Equals(info);
        public bool Equals(KtaneTimeModeInfo other) => other != null && other.Score == Score && other.ScorePerModule == ScorePerModule && other.Origin == Origin;
        public override int GetHashCode() => Ut.ArrayHash(Score, ScorePerModule, Origin);
    }

    sealed class TutorialVideoInfo : IEquatable<TutorialVideoInfo>
    {
        [ClassifyIgnoreIfDefault]
        public string Language;
        [ClassifyIgnoreIfDefault, ClassifyIgnoreIf("")]
        public string Description;
        [ClassifyIgnoreIfDefault]
        public string Url;
        public override bool Equals(object obj) => obj != null && obj is TutorialVideoInfo info && Equals(info);

        public bool Equals(TutorialVideoInfo other) => other != null && Language == other.Language && Description == other.Description && Url == other.Url;
        public override int GetHashCode() => Ut.ArrayHash(Language, Description, Url);
    }

    sealed class DescriptionInfo : IEquatable<DescriptionInfo>
    {
        [ClassifyIgnoreIfDefault]
        public string Language;
        [ClassifyIgnoreIfDefault]
        public string Description;
        [ClassifyIgnoreIfDefault, ClassifyIgnoreIf("")]
        public string Tags;
        public override bool Equals(object obj) => obj != null && obj is DescriptionInfo info && Equals(info);

        public bool Equals(DescriptionInfo other) => other != null && Language == other.Language && Description == other.Description && Tags == other.Tags;
        public override int GetHashCode() => Ut.ArrayHash(Language, Description, Tags);
    }

    sealed class ContributorInfo : IEquatable<ContributorInfo>
    {
        [ClassifyIgnoreIfDefault, EditableField("Developer", "People who developed (programmed) the mod.", AllowedSeparators = new[] { ',', ';' })]
        public string[] Developer;
        [ClassifyIgnoreIfDefault, EditableField("Manual", "People who contributed the manual. (Include only if different from Developer.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] Manual;
        [ClassifyIgnoreIfDefault, ClassifyName("Manual graphics"), EditableField("Manual graphics", "People who contributed graphics for the manual. (Include only if different from Manual contributors.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] ManualGraphics;
        [ClassifyIgnoreIfDefault, ClassifyName("Twitch Plays"), EditableField("Twitch Plays", "People who added Twitch Plays support. (Include only if different from Developer.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] TwitchPlays;
        [ClassifyIgnoreIfDefault, EditableField("Maintainer", "People who are maintaining the mod. (Include only if different from Developer.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] Maintainer;
        [ClassifyIgnoreIfDefault, EditableField("Audio", "People who contributed audio for the mod. (Include only if different from Developer.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] Audio;
        [ClassifyIgnoreIfDefault, EditableField("Modeling", "People who contributed 3D models for the mod. (Include only if different from Developer.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] Modeling;
        [ClassifyIgnoreIfDefault, EditableField("Idea", "People who contributed the original idea for the mod. (Include only if different from both Developer and Manual.)", AllowedSeparators = new[] { ',', ';' })]
        public string[] Idea;

        public string ToAllAuthorString() => new[] { Developer, Manual, ManualGraphics, TwitchPlays, Maintainer, Audio, Modeling, Idea }.Where(authors => authors != null).SelectMany(authors => authors).Distinct().JoinString(", ");
        public string ToAuthorString() => new[] { Developer, Manual }.Where(authors => authors != null).SelectMany(authors => authors).Distinct().JoinString(", ");

        public override bool Equals(object obj) => obj != null && obj is ContributorInfo info && Equals(info);
        private static bool sameArray(string[] one, string[] two) => (one == null && two == null) || (one != null && two != null && one.SequenceEqual(two));
        public bool Equals(ContributorInfo other) => other != null && sameArray(other.Manual, Manual) &&
            sameArray(other.Developer, Developer) && sameArray(other.Maintainer, Maintainer) && sameArray(other.TwitchPlays, TwitchPlays);
        public override int GetHashCode() => Ut.ArrayHash(Manual, Developer, Maintainer, TwitchPlays);
    }

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value
}
