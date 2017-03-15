using System;

namespace KtaneWeb
{
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    internal class KtaneFilterAttribute : Attribute
    {
        public string DataAttributeName { get; private set; }
        public string FilterName { get; private set; }
        public KtaneFilterAttribute(string domId, string filterName)
        {
            DataAttributeName = domId;
            FilterName = filterName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class KtaneFilterOptionAttribute : Attribute
    {
        public string ReadableName { get; private set; }
        public char Accel { get; private set; }

        public KtaneFilterOptionAttribute(string readableName, char accel)
        {
            ReadableName = readableName;
            Accel = accel;
        }
    }
}