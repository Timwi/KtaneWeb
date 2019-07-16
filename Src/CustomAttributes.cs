using System;

namespace KtaneWeb
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class KtaneFilterOptionAttribute : Attribute
    {
        public string ReadableName { get; private set; }
        public char? Accel { get; private set; }

        public KtaneFilterOptionAttribute(string readableName)
        {
            ReadableName = readableName;
            Accel = null;
        }
        public KtaneFilterOptionAttribute(string readableName, char accel)
        {
            ReadableName = readableName;
            Accel = accel;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class KtaneSouvenirInfoAttribute : Attribute
    {
        public char Char { get; private set; }
        public string Tooltip { get; private set; }
        public KtaneSouvenirInfoAttribute(char ch, string tooltip)
        {
            Char = ch;
            Tooltip = tooltip;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class EditableFieldAttribute : Attribute
    {
        public string ReadableName { get; private set; }
        public string Explanation { get; private set; }
        public bool Multiline { get; set; }
        public EditableFieldAttribute(string readable, string explanation = null)
        {
            ReadableName = readable;
            Explanation = explanation;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class EditableIfAttribute : Attribute
    {
        public string OtherField { get; private set; }
        public object[] Values { get; private set; }
        public EditableIfAttribute(string otherField, params object[] values)
        {
            OtherField = otherField;
            Values = values;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class EditableNestedAttribute : Attribute
    {
        public EditableNestedAttribute()
        {
        }
    }
}