using System;
using System.IO;
using RT.TagSoup;
using RT.Util;

namespace KtaneWeb
{
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

    sealed class KtaneModuleInfo : IEquatable<KtaneModuleInfo>
    {
        public string Name;
        public string SortKey;
        public string SteamID;
        public KtaneModuleType Type;
        public KtaneModuleOrigin Origin;
        public KtaneModuleDifficulty DefuserDifficulty;
        public KtaneModuleDifficulty ExpertDifficulty;
        public string Author;
        public string SourceUrl;
        public string TutorialVideoUrl;
        public bool HasTwitchPlaysSupport;

        public object Icon(KtaneWebConfigEntry config) => Path.Combine(config.ModIconDir, Name + ".png")
            .Apply(f => new IMG { class_ = "mod-icon", alt = Name, title = Name, src = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(File.Exists(f) ? f : Path.Combine(config.ModIconDir, "blank.png")))}" });

        public bool Equals(KtaneModuleInfo other)
        {
            return other != null &&
                other.Name == Name &&
                other.SortKey == SortKey &&
                other.SteamID == SteamID &&
                other.Type == Type &&
                other.Origin == Origin &&
                other.DefuserDifficulty == DefuserDifficulty &&
                other.ExpertDifficulty == ExpertDifficulty &&
                other.Author == Author &&
                other.SourceUrl == SourceUrl &&
                other.TutorialVideoUrl == TutorialVideoUrl &&
                other.HasTwitchPlaysSupport == HasTwitchPlaysSupport;
        }

        public override int GetHashCode() => Ut.ArrayHash(HasTwitchPlaysSupport, Type, Origin, DefuserDifficulty, ExpertDifficulty, Name, SortKey, SteamID, Author, SourceUrl, TutorialVideoUrl);
        public override bool Equals(object obj) => Equals(obj as KtaneModuleInfo);
    }

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value
}
