using System;
using System.Linq;
using RT.Util;

namespace KtaneWeb
{
    sealed class KtaneFilter
    {
        public Type EnumType { get; private set; }
        public Func<KtaneModuleInfo, Enum> GetValue { get; private set; }
        public bool Slider { get; private set; }
        public string DataAttributeName { get; private set; }
        public string ReadableName { get; private set; }
        public KtaneFilterOption[] Options { get; private set; }

        public KtaneFilter(string dataAttributeName, string readableName, Type enumType, Func<KtaneModuleInfo, Enum> getValue, bool slider = false)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));

            DataAttributeName = dataAttributeName;
            ReadableName = readableName;
            EnumType = enumType;
            GetValue = getValue;
            Slider = slider;

            Options = Enum.GetValues(enumType).Cast<object>()
                .Select(val => new { Value = val, Attr = ((Enum) val).GetCustomAttribute<KtaneFilterOptionAttribute>() })
                .Select(inf => new KtaneFilterOption { Value = (int) inf.Value, Name = inf.Value.ToString(), ReadableName = inf.Attr?.ReadableName, Accel = inf.Attr?.Accel })
                .ToArray();
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
