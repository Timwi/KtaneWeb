using RT.Json;

namespace KtaneWeb
{
    sealed class Selectable
    {
        public string HumanReadable;
        public char? Accel;
        public string Icon;
        public string PropName;
        public string UrlFunction;
        public string ShowIconFunction;

        public JsonDict ToJson()
        {
            var dict = new JsonDict();

            // Strings
            if (HumanReadable != null) dict["HumanReadable"] = HumanReadable;
            if (Icon != null) dict["Icon"] = Icon;
            if (PropName != null) dict["PropName"] = PropName;

            // Functions
            if (UrlFunction != null) dict["UrlFunction"] = new JsonRaw(UrlFunction);
            if (ShowIconFunction != null) dict["ShowIconFunction"] = new JsonRaw(ShowIconFunction);

            return dict;
        }
    }
}
