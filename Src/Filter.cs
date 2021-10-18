﻿using System;
using System.Linq;
using RT.Json;
using RT.TagSoup;
using RT.Util;

namespace KtaneWeb
{
    abstract class KtaneFilter
    {
        public string ReadableName { get; private set; }
        public string PropName { get; private set; }
        public string FncPropValue { get; private set; }

        public KtaneFilter(string readableName, string propName, string fncPropValue)
        {
            PropName = propName;
            ReadableName = readableName;
            FncPropValue = fncPropValue;
        }

        public abstract JsonDict ToJson();
        public abstract object ToHtml(TranslationInfo translation);
        public abstract bool Matches(KtaneModuleInfo module, JsonDict json);

        public static KtaneFilter Slider<TEnum>(string readableName, string propName, Func<KtaneModuleInfo, TEnum> getValue, string fncPropValue) where TEnum : struct => new KtaneFilterOptionsSlider(propName, readableName, typeof(TEnum), mod => getValue(mod), fncPropValue);
        public static KtaneFilter Slider<TEnum>(string readableName, string propName, Func<KtaneModuleInfo, TEnum?> getValue, string fncPropValue) where TEnum : struct => new KtaneFilterOptionsSlider(propName, readableName, typeof(TEnum), mod => getValue(mod), fncPropValue);
        public static KtaneFilter Checkboxes<TEnum>(string readableName, string propName, Func<KtaneModuleInfo, TEnum> getValue, string fncPropValue) where TEnum : struct => new KtaneFilterOptionsCheckboxes(readableName, propName, fncPropValue, typeof(TEnum), mod => getValue(mod));
        public static KtaneFilter Checkboxes<TEnum>(string readableName, string propName, Func<KtaneModuleInfo, TEnum?> getValue, string fncPropValue) where TEnum : struct => new KtaneFilterOptionsCheckboxes(readableName, propName, fncPropValue, typeof(TEnum), mod => getValue(mod));
        public static KtaneFilter BooleanMultipleSet<TEnum>(string readableName, string propName, Func<KtaneModuleInfo, TEnum> getValue, string fncPropValue) where TEnum : struct => new KtaneFilterFlags(readableName, propName, fncPropValue, typeof(TEnum), mod => getValue(mod));
    }

    abstract class KtaneFilterOptions : KtaneFilter
    {
        public Type EnumType { get; private set; }
        public Func<KtaneModuleInfo, object> GetValue { get; private set; }
        public KtaneFilterOption[] Options { get; private set; }

        public KtaneFilterOptions(string readableName, string propName, string fncPropValue, Type enumType, Func<KtaneModuleInfo, object> getValue)
            : base(readableName, propName, fncPropValue)
        {
            EnumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
            GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));

            Options = Enum.GetValues(enumType).Cast<object>()
                .Select(val => new { Value = val, Attr = ((Enum) val).GetCustomAttribute<KtaneFilterOptionAttribute>() })
                .Where(val => val.Attr != null)
                .Select(inf => new KtaneFilterOption { Value = (int) inf.Value, Name = inf.Value.ToString(), TranslationString = inf.Attr.TranslationString, Accel = inf.Attr.Accel })
                .ToArray();
        }
    }

    sealed class KtaneFilterOptionsCheckboxes : KtaneFilterOptions
    {
        public KtaneFilterOptionsCheckboxes(string readableName, string propName, string fncPropValue, Type enumType, Func<KtaneModuleInfo, object> getValue)
            : base(readableName, propName, fncPropValue, enumType, getValue) { }

        public override JsonDict ToJson() => new JsonDict {
            { "id", PropName },
            { "fnc", new JsonRaw(FncPropValue) },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "checkboxes" }
        };
        public override object ToHtml(TranslationInfo translation) => Ut.NewArray<object>(
            new DIV { class_ = "option-group" }._(
                new H4(ReadableName),
                Options.Select(opt => $"filter-{PropName}-{opt.Name}".Apply(id => new DIV(
                    new INPUT { type = itype.checkbox, class_ = "filter", id = id }, " ",
                    new LABEL { for_ = id, accesskey = opt.Accel.NullOr(a => a.ToString().ToLowerInvariant()) }._(opt.Accel == null ? opt.Translate(translation) : opt.Translate(translation).Accel(opt.Accel.Value)))))));

        public override bool Matches(KtaneModuleInfo module, JsonDict json)
        {
            var str = GetValue(module)?.ToString();
            return str == null || (json.ContainsKey(str) && json[str].GetBool()) || json.All(v => !v.Value.GetBool());
        }
    }

    sealed class KtaneFilterOptionsSlider : KtaneFilterOptions
    {
        public KtaneFilterOptionsSlider(string propName, string readableName, Type enumType, Func<KtaneModuleInfo, object> getValue, string fncPropValue)
            : base(readableName, propName, fncPropValue, enumType, getValue) { }

        public override JsonDict ToJson() => new JsonDict {
            { "id", PropName },
            { "fnc", new JsonRaw(FncPropValue) },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "slider" }
        };
        public override object ToHtml(TranslationInfo translation) => Ut.NewArray<object>(
            new DIV { class_ = "option-group" }._(
                new H4(ReadableName),
                new DIV { id = "filter-" + PropName, class_ = "slider" },
                new DIV { id = "filter-label-" + PropName, class_ = "slider-label" }));

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
        public string TranslationString;
        public char? Accel;
        public int Value;

        public string Translate(TranslationInfo translation) => translation.GetFieldValue<string>(TranslationString);
    }

    // Filter where a mod may have any number of the available flags
    sealed class KtaneFilterFlags : KtaneFilterOptions
    {
        public KtaneFilterFlags(string readableName, string propName, string fncPropValue, Type enumType, Func<KtaneModuleInfo, object> getValue)
            : base(readableName, propName, fncPropValue, enumType, getValue) { }

        public override JsonDict ToJson() => new JsonDict {
            { "id", PropName },
            { "fnc", new JsonRaw(FncPropValue) },
            { "values", Enum.GetValues(EnumType).Cast<Enum>().Select(inf => inf.ToString()).ToJsonList() },
            { "type", "flags" }
        };

        public override object ToHtml(TranslationInfo translation) => new DIV { class_ = "option-group" }._(
            new H4(ReadableName),
            new TABLE(
            Options.Select(opt => new TR(
                new TH(opt.Translate(translation)),
                new TD(
                    new INPUT { type = itype.radio, class_ = "filter", name = $"filter-{PropName}-{opt.Name}", id = $"filter-{PropName}-{opt.Name}-y" }, " ",
                    new LABEL { for_ = $"filter-{PropName}-{opt.Name}-y" }._(translation.flagYes)),
                new TD(
                    new INPUT { type = itype.radio, class_ = "filter", name = $"filter-{PropName}-{opt.Name}", id = $"filter-{PropName}-{opt.Name}-n" }, " ",
                    new LABEL { for_ = $"filter-{PropName}-{opt.Name}-n" }._(translation.flagNo)),
                new TD(
                    new INPUT { type = itype.radio, class_ = "filter", name = $"filter-{PropName}-{opt.Name}", id = $"filter-{PropName}-{opt.Name}-e" }, " ",
                    new LABEL { for_ = $"filter-{PropName}-{opt.Name}-e" }._(translation.flagEither))))));

        public override bool Matches(KtaneModuleInfo module, JsonDict json)
        {
            var flags = (int) GetValue(module);
            foreach (var opt in Options)
			{
                bool hasFlag = (flags & opt.Value) != 0;
                if (json.ContainsKey(opt.Name) && ((json[opt.Name].GetString() == "y" && !hasFlag) || (json[opt.Name].GetString() == "n" && hasFlag)))
                    return false;
            }
            return true;
        }
    }
}
