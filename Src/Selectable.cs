using System;
using RT.Util.Json;

namespace KtaneWeb
{
    sealed class Selectable
    {
        public string HumanReadable;
        public char Accel;
        public Func<KtaneModuleInfo, object> Icon;
        public string DataAttributeName;
        public Func<KtaneModuleInfo, string> DataAttributeValue;
        public Func<KtaneModuleInfo, string> Url;
        public Func<KtaneModuleInfo, bool> ShowIcon;
        public string CssClass = null;
    }
}
