using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RT.TagSoup;
using RT.Util;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

    sealed class KtaneModuleInfo : IEquatable<KtaneModuleInfo>, IComparable<KtaneModuleInfo>, IClassifyObjectProcessor, IClassifyJsonObjectProcessor
    {
        public string Name;
        public string Description;
        public string ModuleID;
        public string SortKey;
        public string SteamID;
        public string Author;

        [ClassifyIgnoreIfDefault]
        public string SourceUrl;
        [ClassifyIgnoreIfDefault]
        public string TutorialVideoUrl;
        [ClassifyIgnoreIfDefault]
        public string Symbol;

        public KtaneModuleType Type = KtaneModuleType.Regular;
        public KtaneModuleOrigin Origin = KtaneModuleOrigin.Mods;
        public KtaneModuleCompatibility Compatibility = KtaneModuleCompatibility.Untested;
        public DateTime Published = DateTime.UtcNow.Date;

        // The following are only relevant for modules (not game rooms, mission packs, etc.)
        [ClassifyIgnoreIfDefault]
        public KtaneModuleDifficulty? DefuserDifficulty;
        [ClassifyIgnoreIfDefault]
        public KtaneModuleDifficulty? ExpertDifficulty;
        [ClassifyIgnoreIfDefault]
        public KtaneSupport? TwitchPlaysSupport;

        [ClassifyIgnoreIfDefault]
        public int? TwitchPlaysScore = null;
        [ClassifyIgnoreIfDefault]
        public string TwitchPlaysSpecial = null;
        [ClassifyIgnoreIfDefault]
        public KtaneSouvenirInfo Souvenir = null;
        [ClassifyIgnoreIfDefault]
        public KtaneSupport RuleSeedSupport = KtaneSupport.NotSupported;

        public object Icon(KtaneWebConfig config) => Path.Combine(config.ModIconDir, Name + ".png")
            .Apply(f => new IMG { class_ = "mod-icon", alt = Name, title = Name, src = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(File.Exists(f) ? f : Path.Combine(config.ModIconDir, "blank.png")))}" });

        public bool Equals(KtaneModuleInfo other)
        {
            return other != null &&
                other.Name == Name &&
                other.Description == Description &&
                other.ModuleID == ModuleID &&
                other.SortKey == SortKey &&
                other.SteamID == SteamID &&
                other.Type == Type &&
                other.Origin == Origin &&
                other.DefuserDifficulty == DefuserDifficulty &&
                other.ExpertDifficulty == ExpertDifficulty &&
                other.Author == Author &&
                other.SourceUrl == SourceUrl &&
                other.TutorialVideoUrl == TutorialVideoUrl &&
                other.TwitchPlaysSupport == TwitchPlaysSupport &&
                other.Compatibility == Compatibility &&
                other.Published == Published &&
                other.Published.Kind == Published.Kind &&
                Equals(other.Souvenir, Souvenir) &&
                other.TwitchPlaysScore == TwitchPlaysScore &&
                other.TwitchPlaysSpecial == TwitchPlaysSpecial &&
                other.Symbol == Symbol &&
                other.RuleSeedSupport == RuleSeedSupport;
        }

        public IEnumerable<string> Differences(KtaneModuleInfo other)
        {
            var diff = new List<string>();
            if (other.Name != Name)
                diff.Add(nameof(Name));
            if (other.Description != Description)
                diff.Add(nameof(Description));
            if (other.ModuleID != ModuleID)
                diff.Add(nameof(ModuleID));
            if (other.SortKey != SortKey)
                diff.Add(nameof(SortKey));
            if (other.SteamID != SteamID)
                diff.Add(nameof(SteamID));
            if (other.Type != Type)
                diff.Add(nameof(Type));
            if (other.Origin != Origin)
                diff.Add(nameof(Origin));
            if (other.DefuserDifficulty != DefuserDifficulty)
                diff.Add(nameof(DefuserDifficulty));
            if (other.ExpertDifficulty != ExpertDifficulty)
                diff.Add(nameof(ExpertDifficulty));
            if (other.Author != Author)
                diff.Add(nameof(Author));
            if (other.SourceUrl != SourceUrl)
                diff.Add(nameof(SourceUrl));
            if (other.TutorialVideoUrl != TutorialVideoUrl)
                diff.Add(nameof(TutorialVideoUrl));
            if (other.TwitchPlaysSupport != TwitchPlaysSupport)
                diff.Add(nameof(TwitchPlaysSupport));
            if (other.Compatibility != Compatibility)
                diff.Add(nameof(Compatibility));
            if (other.Published != Published || other.Published.Kind != Published.Kind)
                diff.Add(nameof(Published));
            if (!Equals(other.Souvenir, Souvenir))
                diff.Add(nameof(Souvenir));
            if (other.TwitchPlaysScore != TwitchPlaysScore)
                diff.Add(nameof(TwitchPlaysScore));
            if (other.TwitchPlaysSpecial != TwitchPlaysSpecial)
                diff.Add(nameof(TwitchPlaysSpecial));
            if (other.Symbol != Symbol)
                diff.Add(nameof(Symbol));
            if (other.RuleSeedSupport != RuleSeedSupport)
                diff.Add(nameof(RuleSeedSupport));
            return diff;
        }

        public override int GetHashCode() => Ut.ArrayHash(Name, SortKey, Symbol, Type, Origin, DefuserDifficulty, ExpertDifficulty, SteamID, Author, SourceUrl, TutorialVideoUrl, Published, Souvenir, TwitchPlaysSupport, TwitchPlaysScore, TwitchPlaysSpecial, RuleSeedSupport);
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
                TwitchPlaysSupport = TwitchPlaysSupport ?? KtaneSupport.NotSupported;
            }
            else
            {
                DefuserDifficulty = null;
                ExpertDifficulty = null;
                TwitchPlaysSupport = null;
            }

            if (TutorialVideoUrl == "")
                TutorialVideoUrl = null;
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
        public KtaneModuleSouvenir Status;
        [ClassifyIgnoreIfDefault]
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

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value
}
