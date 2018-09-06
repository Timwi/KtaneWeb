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
        public string DataAttributeName;
        public string DataAttributeFunction;
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
            if (DataAttributeName != null) dict["DataAttributeName"] = DataAttributeName;
            if (CssClass != null) dict["CssClass"] = CssClass;

            // Functions
            if (IconFunction != null) dict["IconFunction"] = new JsonRaw(IconFunction);
            if (DataAttributeFunction != null) dict["DataAttributeFunction"] = new JsonRaw(DataAttributeFunction);
            if (UrlFunction != null) dict["UrlFunction"] = new JsonRaw(UrlFunction);
            if (ShowIconFunction != null) dict["ShowIconFunction"] = new JsonRaw(ShowIconFunction);

            return dict;
        }
    }
}
