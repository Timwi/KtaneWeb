using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed class KtaneWebConfig : IClassifyObjectProcessor
    {
        public string BaseDir;
        public string HtmlDir;
        public string PdfDir;
        public string ModIconDir = "D:\\Sites\\KTANE\\Icons";

        public string HtmlUrl;
        public string PdfUrl;
        public string ModIconUrl = "/Icons";

        // Icon URLs
        public string HtmlIconUrl;
        public string PdfIconUrl;
        public string PdfEmbellishedIconUrl;
        public string PdfCheatSheetIconUrl;
        public string LogoUrl;
        public string SteamIconUrl;
        public string UnityIconUrl;

        // User/password file for editing
        public string UsersFile;

        public string JavaScriptFile;
        public string CssFile;

        [ClassifyNotNull]
        public KtaneModuleInfo[] KtaneModules = new KtaneModuleInfo[0];

        void IClassifyObjectProcessor.BeforeSerialize() { }
        void IClassifyObjectProcessor.AfterDeserialize()
        {
            if (PdfCheatSheetIconUrl == null)
                PdfCheatSheetIconUrl = PdfEmbellishedIconUrl;
        }
    }
}