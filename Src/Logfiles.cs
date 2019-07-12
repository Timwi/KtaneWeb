using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse uploadLogfile(HttpRequest req)
        {
            if (req.Method != HttpMethod.Post)
                return HttpResponse.PlainText("Only POST requests allowed.", HttpStatusCode._405_MethodNotAllowed);

            if (!req.FileUploads.TryGetValue("log", out var upload))
                return HttpResponse.PlainText("That’s not a valid KTANE logfile.", HttpStatusCode._406_NotAcceptable);

            string entireLog;
            using (var stream = upload.GetStream())
                entireLog = stream.ReadAllBytes().FromUtf8();
            if (!entireLog.Contains("[BombGenerator] Generating bomb with seed"))
                return HttpResponse.PlainText("That’s not a valid KTANE logfile.", HttpStatusCode._406_NotAcceptable);

            string sha1;
            using (var mem = upload.GetStream())
                sha1 = SHA1.Create().ComputeHash(mem).ToHex();
            var filename = sha1 + ".txt";
            var path = Path.Combine(_config.LogfilesDir, filename);
            if (!File.Exists(path))
                lock (this)
                    if (!File.Exists(path))
                        upload.SaveToFile(path);

            return req.Post["noredirect"].Value == "1"
                ? HttpResponse.PlainText(req.Url.WithPathParent().WithPathOnly($"/lfa#file={sha1}").ToFull())
                : HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly($"/More/Logfile%20Analyzer.html#file={sha1}"));
        }

        private HttpResponse findLogfile(HttpRequest req)
        {
            var keywords = req.Url.QueryValues("find").ToArray();
            if (keywords.Length < 1)
                return HttpResponse.PlainText("No search keywords specified.");
            var regexes = keywords.Select(kw => new Regex($@"\[BombGenerator\] Selected {Regex.Escape(kw)} \(.* \(\w+Component\)\)", RegexOptions.Compiled)).ToArray();

            lock (this)
            {
                var list = new DirectoryInfo(_config.LogfilesDir)
                    .EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(fi => fi.LastWriteTimeUtc)
                    .Take(100)
                    .Where(fi => File.ReadAllText(fi.FullName).Apply(text => regexes.All(re => re.IsMatch(text))))
                    .Take(5)
                    .ToArray();

                string url(FileInfo f) => $"More/Logfile%20Analyzer.html#file={Path.GetFileNameWithoutExtension(f.Name)};module={keywords[0]}";

                return list.Length == 0
                    ? HttpResponse.Html(new HTML(
                        new HEAD(new TITLE("Logfile search results"), new META { charset = "utf-8" }),
                        new BODY(
                            new H1($"No logfiles found containing module ID{(keywords.Length == 1 ? null : "s")} {keywords.Select(kw => $"“{kw}”").JoinString(" and ")}."))))
                    : list.Length == 1
                        ? HttpResponse.Redirect(url(list[0]))
                        : HttpResponse.Html(new HTML(
                            new HEAD(new TITLE("Logfile search results"), new META { charset = "utf-8" }),
                            new BODY(
                                new H1($"Recent logfiles containing module ID{(keywords.Length == 1 ? null : "s")} {keywords.Select(kw => $"“{kw}”").JoinString(" and ")}"),
                                new TABLE(
                                    new TR(new TH("Date/time"), new TH("Logfile")),
                                    list.Select(entry => new TR(new TD(entry.LastWriteTimeUtc.ToIsoString(IsoDatePrecision.Days)), new TD(new A { href = url(entry) }._(Path.GetFileNameWithoutExtension(entry.Name)))))))));
            }
        }
    }
}
