using System;
using System.IO;
using System.Text.RegularExpressions;
using RT.TagSoup;
using RT.Util;
using RT.Util.Serialization;

namespace KtaneWeb
{
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

    sealed class KtaneModuleInfo : IEquatable<KtaneModuleInfo>, IComparable<KtaneModuleInfo>, IClassifyObjectProcessor
    {
        public string Name;
        public string Description;
        public string ModuleID;
        public string SortKey;
        public string SteamID;
        public string Author;
        public string SourceUrl;
        public string TutorialVideoUrl;
        public KtaneModuleType Type = KtaneModuleType.Regular;
        public KtaneModuleOrigin Origin = KtaneModuleOrigin.Mods;
        public KtaneModuleCompatibility Compatibility = KtaneModuleCompatibility.Untested;

        // The following are only relevant for modules (not game rooms, mission packs, etc.)
        [ClassifyIgnoreIfDefault]
        public KtaneModuleDifficulty? DefuserDifficulty;
        [ClassifyIgnoreIfDefault]
        public KtaneModuleDifficulty? ExpertDifficulty;
        [ClassifyIgnoreIfDefault]
        public KtaneTwitchPlays? TwitchPlaysSupport;

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
                other.Compatibility == Compatibility;
        }

        public override int GetHashCode() => Ut.ArrayHash(TwitchPlaysSupport, Type, Origin, DefuserDifficulty, ExpertDifficulty, Name, SortKey, SteamID, Author, SourceUrl, TutorialVideoUrl);
        public override bool Equals(object obj) => Equals(obj as KtaneModuleInfo);

        void IClassifyObjectProcessor.BeforeSerialize() { }
        void IClassifyObjectProcessor.AfterDeserialize()
        {
            if (SortKey == null)
                SortKey = Regex.Replace(Name, @"^The |[^a-zA-Z0-9]", "", RegexOptions.IgnoreCase).ToUpperInvariant();

            if (Type == KtaneModuleType.Regular || Type == KtaneModuleType.Needy)
            {
                DefuserDifficulty = DefuserDifficulty ?? KtaneModuleDifficulty.Easy;
                ExpertDifficulty = ExpertDifficulty ?? KtaneModuleDifficulty.Easy;
                TwitchPlaysSupport = TwitchPlaysSupport ?? KtaneTwitchPlays.NotSupported;
            }
            else
            {
                DefuserDifficulty = null;
                ExpertDifficulty = null;
                TwitchPlaysSupport = null;
            }
        }

        int IComparable<KtaneModuleInfo>.CompareTo(KtaneModuleInfo other) => other == null ? 1 : SortKey == null ? (other.SortKey == null ? 0 : -1) : other.SortKey == null ? 1 : SortKey.CompareTo(other.SortKey);
    }

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value
}
