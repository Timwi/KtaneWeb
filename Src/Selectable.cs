using System;

namespace KtaneWeb
{
    sealed class Selectable
    {
        public string HumanReadable;
        public char Accel;
        public string IconUrl;
        public string DataAttributeName;
        public Func<bool> FileExists;
        public Func<KtaneModuleInfo, string> Url;
        public Func<KtaneModuleInfo, string> AltUrl;
    }
}
