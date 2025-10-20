using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RT.TagSoup;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    static class Extensions
    {
        public static object Accel(this string str, char? accel)
        {
            if (accel == null)
                return str;
            var pos = str.IndexOf(accel.Value);
            if (pos == -1)
                pos = str.IndexOf(accel.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
            if (pos == -1)
                return new object[] { str, " (", new KBD(char.ToUpperInvariant(accel.Value)), ")" };
            return new object[] { str.Substring(0, pos), new KBD(str.Substring(pos, 1)), str.Substring(pos + 1) };
        }

        public static string ToReadable(this KtaneModuleDifficulty difficulty)
        {
            return Regex.Matches(difficulty.ToString(), @"\p{Lu}\p{Ll}*").Cast<Match>().Select(m => m.Value.ToLowerInvariant()).JoinString(" ");
        }

        public static Tag AddData<T>(this Tag tag, IEnumerable<T> infos, Func<T, string> dataName, Func<T, object> dataValue)
        {
            return infos.Aggregate(tag, (prev, next) => prev.Data(dataName(next), dataValue(next)));
        }

        public static string Pluralize(this decimal number, string singular) => $"{number} {(number == 1 ? singular : singular + "s")}";
    }
}
