using System;
using System.IO;
using System.Text.RegularExpressions;
using RT.TagSoup;
using RT.Util;
using RT.Json;
using RT.Serialization;

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
        [EditableField("Description", "A concise description of what sets this module or widget apart from others. Include tags at the end.")]
        public string Description;
        [EditableField("Module ID", "The ID that mission makers need for this module. This is the same as the ModuleType property on the KMBombModule component.")]
        [EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        public string ModuleID;
        [EditableField("Sort key", "The name of the module or widget in all-caps, without spaces, and without initial “The”.")]
        public string SortKey;
        [EditableField("Steam ID", "The numerical ID of the Steam Workshop item.")]
        public string SteamID;
        [EditableField("Author", "A comma-separated list of contributors to the development of the module or widget.")]
        public string Author;

        [ClassifyIgnoreIfDefault, EditableField("Source code", "A link to the source code of the module or widget, usually a link to a GitHub repository.")]
        public string SourceUrl;
        [ClassifyIgnoreIfDefault, EditableField("Tutorial video", "A link to a tutorial video, if available (usually on YouTube).")]
        public string TutorialVideoUrl;
        [ClassifyIgnoreIfDefault, EditableField("Symbol", "A symbol for the Periodic Table of Modules. Only the first letter will be capitalized."), EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        public string Symbol;

        [EditableField("Compatibility", "Specify if the module or widget has any known issues.\nUse “Problematic” if the issues are cosmetic.\nUse “Inconsistent” if only rare and specific case cause undeserved strikes.\nUse “Unplayable” if a bug causes undeserved strikes.")]
        public KtaneModuleCompatibility Compatibility = KtaneModuleCompatibility.Untested;
        [ClassifyIgnoreIfDefault, EditableField("Explain", "Explain the Compatibility setting above."), EditableIf(nameof(Compatibility), KtaneModuleCompatibility.Problematic, KtaneModuleCompatibility.Inconsistent, KtaneModuleCompatibility.Unplayable)]
        public string CompatibilityExplanation = null;
        [EditableField("Published", "The date of publication.")]
        public DateTime Published = DateTime.UtcNow.Date;

        // The following are only relevant for modules (not widgets)
        [ClassifyIgnoreIfDefault, EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        [EditableField("Defuser difficulty", "An approximate difficulty rating for the defuser.")]
        public KtaneModuleDifficulty? DefuserDifficulty;
        [ClassifyIgnoreIfDefault, EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        [EditableField("Expert difficulty", "An approximate difficulty rating for the expert.")]
        public KtaneModuleDifficulty? ExpertDifficulty;

        [ClassifyIgnoreIfDefault, EditableField("Rule-seed", "Does the module vary its rules and manual under the Rule Seed Modifier?")]
        public KtaneSupport RuleSeedSupport = KtaneSupport.NotSupported;

        // Specifies which modules this module should ignore. Applies to boss and semi-boss modules such as Forget Me Not, Alchemy, Hogwarts, etc.
        [ClassifyIgnoreIfDefault, ClassifyIgnoreIfEmpty, EditableField("Ignore list", "Use only for boss modules. Specify which other modules this module should ignore.")]
        public string[] Ignore = null;

        [ClassifyIgnoreIfDefault, EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        [EditableField("Translation of", "Only enter this if this module is a translation of another module. Specify the original name of the other module (e.g., “The Button”). It will not be listed separately on the website.")]
        public string TranslationOf = null;

        [ClassifyIgnoreIfDefault, EditableNested, EditableField("Souvenir", "Uncheck for modules that have not been assessed."), EditableIf(nameof(Type), KtaneModuleType.Regular, KtaneModuleType.Needy)]
        public KtaneSouvenirInfo Souvenir = null;

        [ClassifyIgnoreIfDefault, EditableField("Mystery Module compatibility", "Specify how Mystery Module may affect this module. Use “MM must not hide this” if this module requires other modules to be solved earlier (e.g. Encryption Bingo, Hogwarts). Use “MM must not require this” if the module depends on whether or not other modules are on the bomb (e.g. Free Parking, Mafia). Use “MM must not use this at all” if both is the case. Use “MM must auto-solve” if this module imposes a solve order on other modules (e.g. Organization, Turn the Keys).")]
        public KtaneMysteryModuleCompatibility MysteryModule = KtaneMysteryModuleCompatibility.NoConflict;

        // This information is imported from a spreadsheet, so not serialized in JSON.
        [ClassifyIgnoreIfDefault, EditableField(null)]
        public KtaneTwitchPlaysInfo TwitchPlays = null;

        public object Icon(KtaneWebConfig config) => Path.Combine(config.BaseDir, "Icons", Name + ".png")
            .Apply(f => new IMG { class_ = "mod-icon", alt = Name, title = Name, src = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(File.Exists(f) ? f : Path.Combine(config.BaseDir, "Icons", "blank.png")))}" });

        public bool Equals(KtaneModuleInfo other)
        {
            return other != null &&
                other.Author == Author &&
                other.Compatibility == Compatibility &&
                other.DefuserDifficulty == DefuserDifficulty &&
                other.Description == Description &&
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
                other.TutorialVideoUrl == TutorialVideoUrl &&
                Equals(other.TwitchPlays, TwitchPlays) &&
                other.Type == Type;
        }

        public override int GetHashCode() => Ut.ArrayHash(Author, Compatibility, DefuserDifficulty, Description, ExpertDifficulty, Name, Origin, Published, RuleSeedSupport, SortKey, SourceUrl, Souvenir, SteamID, Symbol, TutorialVideoUrl, TwitchPlays, Type);
        public override bool Equals(object obj) => Equals(obj as KtaneModuleInfo);

        int IComparable<KtaneModuleInfo>.CompareTo(KtaneModuleInfo other) => other == null ? 1 : SortKey == null ? (other.SortKey == null ? 0 : -1) : other.SortKey == null ? 1 : SortKey.CompareTo(other.SortKey);

        void IClassifyObjectProcessor.AfterDeserialize()
        {
            if (SortKey == null || SortKey == "")
                SortKey = Regex.Replace(Name, @"^The |[^a-zA-Z0-9]", "", RegexOptions.IgnoreCase).ToUpperInvariant();

            if (Type == KtaneModuleType.Regular || Type == KtaneModuleType.Needy)
            {
                DefuserDifficulty = DefuserDifficulty ?? KtaneModuleDifficulty.Easy;
                ExpertDifficulty = ExpertDifficulty ?? KtaneModuleDifficulty.Easy;
            }
            else
            {
                DefuserDifficulty = null;
                ExpertDifficulty = null;
                TwitchPlays = null;
                RuleSeedSupport = KtaneSupport.NotSupported;
            }

            if (TutorialVideoUrl == "")
                TutorialVideoUrl = null;

            if (Souvenir != null && Souvenir.Status == KtaneModuleSouvenir.Unexamined)
                Souvenir = null;
            else if (Souvenir != null && Souvenir.Status != KtaneModuleSouvenir.Considered)
                Souvenir.Explanation = null;

            if (TwitchPlays != null)
                TwitchPlays.NeedyScoring = Type == KtaneModuleType.Needy ? (TwitchPlays.NeedyScoring ?? KtaneTwitchPlaysNeedyScoring.Solves).Nullable() : null;

            if (Ignore != null && Ignore.Length == 0)
                Ignore = null;

            if (Symbol != null && Symbol.Length > 0)
                Symbol = Symbol.Substring(0, 1).ToUpperInvariant() + Symbol.Substring(1).ToLowerInvariant();
        }

        void IClassifyObjectProcessor<JsonValue>.AfterSerialize(JsonValue element)
        {
            if (element is JsonDict && element.ContainsKey("Published") && element["Published"].GetStringSafe()?.EndsWith("Z") == true)
                element["Published"] = element["Published"].GetString().Apply(s => s.Remove(s.Length - 1));
        }

        void IClassifyObjectProcessor.BeforeSerialize() { }
        void IClassifyObjectProcessor<JsonValue>.BeforeSerialize() { }
        void IClassifyObjectProcessor<JsonValue>.BeforeDeserialize(JsonValue element) { }
        void IClassifyObjectProcessor<JsonValue>.AfterDeserialize(JsonValue element) { }
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

        public override bool Equals(object obj) => obj != null && obj is KtaneSouvenirInfo && Equals((KtaneSouvenirInfo) obj);
        public bool Equals(KtaneSouvenirInfo other) => other != null && other.Status == Status && other.Explanation == Explanation;
        public override int GetHashCode() => Ut.ArrayHash(Status, Explanation);
    }

    sealed class KtaneTwitchPlaysInfo : IEquatable<KtaneTwitchPlaysInfo>
    {
        [ClassifyIgnoreIfDefault, EditableField("Score", "For regular modules, the score for solving it. For needy modules, depends on the scoring method.")]
        public decimal? Score;
        [ClassifyIgnoreIfDefault, EditableIf(nameof(KtaneModuleInfo.Type), KtaneModuleType.Regular), EditableField("Score per module", "For boss modules, a score value that is multiplied by the total number of modules on the bomb.")]
        public decimal? ScorePerModule;          // for boss modules like FMN
        [ClassifyIgnoreIfDefault, EditableIf(nameof(KtaneModuleInfo.Type), KtaneModuleType.Regular), EditableField("Score-per-module cap", "For boss modules, a maximum number of modules up to which the boss module is scored. Use 0 if there is no cap.")]
        public int ScorePerModuleCap;    // for modules like FE that cap out at a certain module count
        [ClassifyIgnoreIfDefault, EditableIf(nameof(KtaneModuleInfo.Type), KtaneModuleType.Needy), EditableField("Needy scoring", "How are the points for a needy module calculated?")]
        public KtaneTwitchPlaysNeedyScoring? NeedyScoring = null;

        // Human-readable explanation for special scoring (e.g. Souvenir)
        [ClassifyIgnoreIfDefault, EditableField("Special scoring", "Explain in words if the module’s scoring is special (e.g. Souvenir).")]
        public string ScoreExplanation;

        [ClassifyIgnoreIfDefault, EditableField("Tag position", "Overrides the position of the tag with the module code and claimant name. “Automatic” will usually place it where the status light is.")]
        public KtaneTwitchPlaysTagPosition TagPosition = KtaneTwitchPlaysTagPosition.Automatic;

        // Specifies whether the module can be pinned in TP by a normal user. (Moderators can always pin any module.)
        [ClassifyIgnoreIfDefault, EditableField("Auto-pin", "Tick if this module should be automatically pinned at the start of a game. This will also allow regular users to re-pin the module, and it will cause the module to be announced in the chat.")]
        public bool AutoPin = false;

        [ClassifyIgnoreIfDefault, EditableField("Help text", "ONLY if the module doesn’t already provide an adequate help message detailing its commands, provide a help message here.")]
        public string HelpText = null;

        public override bool Equals(object obj) => obj != null && obj is KtaneTwitchPlaysInfo && Equals((KtaneTwitchPlaysInfo) obj);
        public bool Equals(KtaneTwitchPlaysInfo other) => other != null &&
            other.Score == Score && other.ScorePerModule == ScorePerModule && other.ScorePerModuleCap == ScorePerModuleCap &&
            other.ScoreExplanation == ScoreExplanation && other.TagPosition == TagPosition && other.NeedyScoring == NeedyScoring && other.AutoPin == AutoPin;
        public override int GetHashCode() => Ut.ArrayHash(Score, ScorePerModule, ScorePerModuleCap, ScoreExplanation, TagPosition, NeedyScoring, AutoPin);
    }

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value
}
