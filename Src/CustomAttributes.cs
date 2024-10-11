using System;
using RT.Util;

namespace KtaneWeb
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class KtaneFilterOptionAttribute : Attribute
    {
        public string TranslationString { get; private set; }
        public char? Accel { get; private set; }

        public KtaneFilterOptionAttribute(string translationString)
        {
            TranslationString = translationString;
            Accel = null;
        }
        public KtaneFilterOptionAttribute(string translationString, char accel)
        {
            TranslationString = translationString;
            Accel = accel;
        }

        public string Translate(TranslationInfo translation) => translation.GetFieldValue<string>(TranslationString);
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class KtaneSouvenirInfoAttribute(char ch, string tooltip) : Attribute
    {
        public char Char { get; private set; } = ch;
        public string Tooltip { get; private set; } = tooltip;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class EditableFieldAttribute : Attribute
    {
        public string ReadableName { get; private set; }
        public string Explanation { get; private set; }
        public bool Multiline { get; set; }
        public char[] AllowedSeparators { get; set; }
        public char[] AllowedDictSeparators { get; set; }
        public string DefaultKey { get; set; }
        public EditableFieldAttribute(string readable, string explanation = null)
        {
            ReadableName = readable;
            Explanation = explanation;
            AllowedSeparators ??= [';'];
            AllowedDictSeparators ??= [':'];
            DefaultKey ??= "default";
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class EditableIfAttribute(string otherField, params object[] values) : Attribute
    {
        public string OtherField { get; private set; } = otherField;
        public object[] Values { get; private set; } = values;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class EditableNestedAttribute : Attribute
    {
        public EditableNestedAttribute()
        {
        }
    }

    internal sealed class EditableHelpAttribute(string translationString) : Attribute
    {
        public string TranslationString { get; private set; } = translationString;
        public string Translate(TranslationInfo translation) => translation.GetFieldValue<string>(TranslationString);
    }
}