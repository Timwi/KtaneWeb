using RT.TagSoup;

namespace KtaneWeb
{
    static class Extensions
    {
        public static object Accel(this string str, char accel)
        {
            var pos = str.IndexOf(accel);
            if (pos == -1)
                return new object[] { str, " (", new KBD(char.ToLowerInvariant(accel)), ")" };
            return new object[] { str.Substring(0, pos), new KBD(accel), str.Substring(pos + 1) };
        }
    }
}
