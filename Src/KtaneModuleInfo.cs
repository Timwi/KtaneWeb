using System;
using System.IO;
using RT.TagSoup;
using RT.Util;

namespace KtaneWeb
{
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

    sealed class KtaneModuleInfo
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

        public object Icon(KtaneWebConfig config) => Path.Combine(config.ModIconDir, Name + ".png")
            .Apply(f => new IMG { class_ = "mod-icon", alt = Name, title = Name, src = $"data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(File.Exists(f) ? f : Path.Combine(config.ModIconDir, "blank.png")))}" });

        internal void SetDifficulty(KtaneModuleDifficulty exp, KtaneModuleDifficulty def)
        {
            ExpertDifficulty = exp;
            DefuserDifficulty = def;
        }
    }

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value
}
