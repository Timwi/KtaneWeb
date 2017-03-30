using System;

namespace KtaneWeb
{
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