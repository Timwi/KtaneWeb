using System;
using System.IO;
using System.Linq;
using RT.TagSoup;
using RT.Util.Json;

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

        public static JsonList EnumerateSheets(this DirectoryInfo dir, string before, string after, KtaneWebConfig config)
        {
            if (dir == null)
                throw new ArgumentNullException(nameof(dir));

            return dir.EnumerateFiles(before + "*" + after).OrderBy(f => f.Name.Remove(f.Name.Length - f.Extension.Length)).Select(f => new JsonDict { { "Name", f.Name.Substring(before.Length, f.Name.Length - before.Length - after.Length) }, { "Url", config.PdfUrl + "/" + f.Name } }).ToJsonList();
        }
    }
}
