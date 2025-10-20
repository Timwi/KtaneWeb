using RT.Json;

namespace KtaneWeb
{
    sealed class Selectable
    {
        public char? Accel;
        public string PropName;
        public string HumanReadable;
        public string HumanReadableFunction;
        public string IconFunction;
        public string UrlFunction;
        public string ShowIconFunction;

        public JsonDict ToJson()
        {
            var dict = new JsonDict();

            // String
            if (PropName != null) dict[nameof(PropName)] = PropName;
            if (HumanReadable != null) dict[nameof(HumanReadable)] = HumanReadable;

            // Functions
            if (HumanReadableFunction != null) dict[nameof(HumanReadableFunction)] = new JsonRaw(HumanReadableFunction);
            if (IconFunction != null) dict[nameof(IconFunction)] = new JsonRaw(IconFunction);
            if (UrlFunction != null) dict[nameof(UrlFunction)] = new JsonRaw(UrlFunction);
            if (ShowIconFunction != null) dict[nameof(ShowIconFunction)] = new JsonRaw(ShowIconFunction);

            return dict;
        }
    }
}
