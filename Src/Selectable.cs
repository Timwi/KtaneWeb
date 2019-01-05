using System;
using RT.Util.Json;

namespace KtaneWeb
{
    sealed class Selectable
    {
        public string HumanReadable;
        public char? Accel;
        public string Icon;
        public string IconFunction;
        public string PropName;
        public string FncPropValue;
        public Func<KtaneModuleInfo, bool> HasValue;
        public string UrlFunction;
        public string ShowIconFunction;
        public string CssClass = null;

        public JsonDict ToJson()
        {
            var dict = new JsonDict();

            // Strings
            if (HumanReadable != null) dict["HumanReadable"] = HumanReadable;
            if (Icon != null) dict["Icon"] = Icon;
            if (PropName != null) dict["PropName"] = PropName;
            if (CssClass != null) dict["CssClass"] = CssClass;

            // Functions
            if (IconFunction != null) dict["IconFunction"] = new JsonRaw(IconFunction);
            if (FncPropValue != null) dict["FncPropValue"] = new JsonRaw(FncPropValue);
            if (UrlFunction != null) dict["UrlFunction"] = new JsonRaw(UrlFunction);
            if (ShowIconFunction != null) dict["ShowIconFunction"] = new JsonRaw(ShowIconFunction);

            return dict;
        }
    }
}
