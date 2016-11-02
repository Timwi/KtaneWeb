using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed class KtaneWebConfig
    {
        public string HtmlDir;
        public string PdfDir;
        public string HtmlUrl;
        public string PdfUrl;
        public string HtmlIconUrl;
        public string PdfIconUrl;
        public string LogoUrl = "HTML/img/ktane-logo.png";

        [ClassifyNotNull]
        public KtaneModuleInfo[] KtaneModules = new KtaneModuleInfo[0];
    }
}