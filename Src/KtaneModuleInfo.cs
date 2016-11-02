using RT.Util;

namespace KtaneWeb
{
    public sealed class KtaneModuleInfo
    {
        public string Name;
        public string SteamID;
        public KtaneModuleType Type;
        public KtaneModuleOrigin Origin;
        public string Author;
        public string SourceUrl;

        public string SteamUrl => SteamID?.Apply(s => $"http://steamcommunity.com/sharedfiles/filedetails/?id={s}");
    }
}