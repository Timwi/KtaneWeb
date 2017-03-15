using System;
using System.Linq;
using System.Reflection;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    sealed class KtaneFilter
    {
        public Type EnumType { get; private set; }
        public Func<KtaneModuleInfo, Enum> GetValue { get; private set; }

        private KtaneFilterAttribute Attribute;

        public KtaneFilter(Type enumType, Func<KtaneModuleInfo, Enum> getValue)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));
            if (getValue == null)
                throw new ArgumentNullException(nameof(getValue));

            Attribute = enumType.GetCustomAttributes<KtaneFilterAttribute>().FirstOrDefault();
            if (Attribute == null)
                throw new InvalidOperationException("The specified filter does not have a KtaneFilterAttribute.");

            EnumType = enumType;
            GetValue = getValue;
        }

        public string DataAttributeName => Attribute.DataAttributeName;
    }
}
