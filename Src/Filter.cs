using System;
using System.Linq;
using RT.TagSoup;
using RT.Util;
using RT.Util.Json;

namespace KtaneWeb
{
    abstract class KtaneFilter
    {
        public string DataAttributeName { get; private set; }
        public string ReadableName { get; private set; }

        public KtaneFilter(string dataAttributeName, string readableName)
        {
            DataAttributeName = dataAttributeName;
            ReadableName = readableName;
        }

        public abstract JsonDict ToJson();
        public abstract object ToHtml();
        public abstract string GetDataAttributeValue(KtaneModuleInfo mod);

        public static KtaneFilter Slider<TEnum>(string dataAttributeName, string readableName, Func<KtaneModuleInfo, TEnum> getValue) => new KtaneFilterOptionsSlider(dataAttributeName, readableName, typeof(TEnum), mod => getValue(mod));
        public static KtaneFilter Checkboxes<TEnum>(string dataAttributeName, string readableName, Func<KtaneModuleInfo, TEnum> getValue) => new KtaneFilterOptionsCheckboxes(dataAttributeName, readableName, typeof(TEnum), mod => getValue(mod));
    }

    abstract class KtaneFilterOptions : KtaneFilter
    {
        public Type EnumType { get; private set; }
        public Func<KtaneModuleInfo, object> GetValue { get; private set; }
        public KtaneFilterOption[] Options { get; private set; }

        public KtaneFilterOptions(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue) : base(dataAttributeName, readableName)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));

            EnumType = enumType;
            GetValue = getValue;

            Options = Enum.GetValues(enumType).Cast<object>()
                .Select(val => new { Value = val, Attr = ((Enum) val).GetCustomAttribute<KtaneFilterOptionAttribute>() })
                .Select(inf => new KtaneFilterOption { Value = (int) inf.Value, Name = inf.Value.ToString(), ReadableName = inf.Attr?.ReadableName, Accel = inf.Attr?.Accel })
                .ToArray();
        }
    }

    sealed class KtaneFilterOptionsCheckboxes : KtaneFilterOptions
    {
        public KtaneFilterOptionsCheckboxes(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue) : base(dataAttributeName, readableName, enumType, getValue) { }
        public override JsonDict ToJson() => new JsonDict {
            { "id", DataAttributeName },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "checkboxes" }
        };
        public override object ToHtml() => Ut.NewArray<object>(
            new H4(ReadableName, ":"),
            Options.Select(opt => new DIV(
              new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-" + opt.Name }, " ",
              new LABEL { for_ = "filter-" + opt.Name, accesskey = opt.Accel.NullOr(a => a.ToString().ToLowerInvariant()) }._(opt.Accel == null ? opt.ReadableName : opt.ReadableName.Accel(opt.Accel.Value)))));
        public override string GetDataAttributeValue(KtaneModuleInfo mod) => GetValue(mod).ToString();
    }
    sealed class KtaneFilterOptionsSlider : KtaneFilterOptions
    {
        public KtaneFilterOptionsSlider(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue) : base(dataAttributeName, readableName, enumType, getValue) { }
        public override JsonDict ToJson() => new JsonDict {
            { "id", DataAttributeName },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "slider" }
        };
        public override object ToHtml() => Ut.NewArray<object>(
            new H4(ReadableName, ":"),
            new DIV { id = "filter-" + DataAttributeName, class_ = "slider" },
            new DIV { id = "filter-label-" + DataAttributeName, class_ = "slider-label" });
        public override string GetDataAttributeValue(KtaneModuleInfo mod) => GetValue(mod).ToString();
    }

    sealed class KtaneFilterOption
    {
        public string Name;
        public string ReadableName;
        public char? Accel;
        public int Value;
    }
}
