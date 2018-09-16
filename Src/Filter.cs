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
        public string DataAttributeFunction { get; private set; }

        public KtaneFilter(string dataAttributeName, string readableName, string dataAttributeFunction)
        {
            DataAttributeName = dataAttributeName;
            ReadableName = readableName;
            DataAttributeFunction = dataAttributeFunction;
        }

        public abstract JsonDict ToJson();
        public abstract object ToHtml();
        public abstract string GetDataAttributeValue(KtaneModuleInfo mod);
        public abstract bool Matches(KtaneModuleInfo module, JsonDict json);

        public static KtaneFilter Slider<TEnum>(string dataAttributeName, string readableName, Func<KtaneModuleInfo, TEnum> getValue, string dataAttributeFunction) where TEnum : struct => new KtaneFilterOptionsSlider(dataAttributeName, readableName, typeof(TEnum), mod => getValue(mod), dataAttributeFunction);
        public static KtaneFilter Slider<TEnum>(string dataAttributeName, string readableName, Func<KtaneModuleInfo, TEnum?> getValue, string dataAttributeFunction) where TEnum : struct => new KtaneFilterOptionsSlider(dataAttributeName, readableName, typeof(TEnum), mod => getValue(mod), dataAttributeFunction);
        public static KtaneFilter Checkboxes<TEnum>(string dataAttributeName, string readableName, Func<KtaneModuleInfo, TEnum> getValue, string dataAttributeFunction) where TEnum : struct => new KtaneFilterOptionsCheckboxes(dataAttributeName, readableName, typeof(TEnum), mod => getValue(mod), dataAttributeFunction);
        public static KtaneFilter Checkboxes<TEnum>(string dataAttributeName, string readableName, Func<KtaneModuleInfo, TEnum?> getValue, string dataAttributeFunction) where TEnum : struct => new KtaneFilterOptionsCheckboxes(dataAttributeName, readableName, typeof(TEnum), mod => getValue(mod), dataAttributeFunction);
    }

    abstract class KtaneFilterOptions : KtaneFilter
    {
        public Type EnumType { get; private set; }
        public Func<KtaneModuleInfo, object> GetValue { get; private set; }
        public KtaneFilterOption[] Options { get; private set; }

        public KtaneFilterOptions(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue, string dataAttributeFunction) : base(dataAttributeName, readableName, dataAttributeFunction)
        {
            EnumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
            GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));

            Options = Enum.GetValues(enumType).Cast<object>()
                .Select(val => new { Value = val, Attr = ((Enum) val).GetCustomAttribute<KtaneFilterOptionAttribute>() })
                .Where(val => val.Attr != null)
                .Select(inf => new KtaneFilterOption { Value = (int) inf.Value, Name = inf.Value.ToString(), ReadableName = inf.Attr.ReadableName, Accel = inf.Attr.Accel })
                .ToArray();
        }
    }

    sealed class KtaneFilterOptionsCheckboxes : KtaneFilterOptions
    {
        public KtaneFilterOptionsCheckboxes(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue, string dataAttributeFunction) : base(dataAttributeName, readableName, enumType, getValue, dataAttributeFunction) { }
        public override JsonDict ToJson() => new JsonDict {
            { "id", DataAttributeName },
            { "fnc", new JsonRaw(DataAttributeFunction) },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "checkboxes" }
        };
        public override object ToHtml() => Ut.NewArray<object>(
            new H4(ReadableName, ":"),
            Options.Select(opt => $"filter-{DataAttributeName}-{opt.Name}".Apply(id => new DIV(
                new INPUT { type = itype.checkbox, class_ = "filter", id = id }, " ",
                new LABEL { for_ = id, accesskey = opt.Accel.NullOr(a => a.ToString().ToLowerInvariant()) }._(opt.Accel == null ? opt.ReadableName : opt.ReadableName.Accel(opt.Accel.Value))))));
        public override string GetDataAttributeValue(KtaneModuleInfo mod) => GetValue(mod)?.ToString();

        public override bool Matches(KtaneModuleInfo module, JsonDict json)
        {
            var val = GetValue(module);
            if (val == null)
                return true;
            var str = val.ToString();
            var dic = json.GetDict();
            return dic.ContainsKey(str) && dic[str].GetBool();
        }
    }

    sealed class KtaneFilterOptionsSlider : KtaneFilterOptions
    {
        public KtaneFilterOptionsSlider(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue, string dataAttributeFunction) : base(dataAttributeName, readableName, enumType, getValue, dataAttributeFunction) { }
        public override JsonDict ToJson() => new JsonDict {
            { "id", DataAttributeName },
            { "fnc", new JsonRaw(DataAttributeFunction) },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "slider" }
        };
        public override object ToHtml() => Ut.NewArray<object>(
            new H4(ReadableName, ":"),
            new DIV { id = "filter-" + DataAttributeName, class_ = "slider" },
            new DIV { id = "filter-label-" + DataAttributeName, class_ = "slider-label" });
        public override string GetDataAttributeValue(KtaneModuleInfo mod) => GetValue(mod)?.ToString();

        public override bool Matches(KtaneModuleInfo module, JsonDict json)
        {
            var val = GetValue(module);
            if (val == null)
                return true;
            try
            {
                return (int) val >= json["min"].GetInt() && (int) val <= json["max"].GetInt();
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }
    }

    sealed class KtaneFilterOption
    {
        public string Name;
        public string ReadableName;
        public char? Accel;
        public int Value;
    }
}
