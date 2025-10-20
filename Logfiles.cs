using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        //public const int MaxLogfileSize = 1 * 1024 * 1024;

        private HttpResponse uploadLogfile(HttpRequest req)
        {
            if (req.Method != HttpMethod.Post)
                return HttpResponse.PlainText("Only POST requests allowed.", HttpStatusCode._405_MethodNotAllowed);

            if (!req.FileUploads.TryGetValue("log", out var upload))
                return HttpResponse.PlainText("That’s not a valid KTANE logfile.", HttpStatusCode._406_NotAcceptable);

            using (var stream = upload.GetStream())
            {
                //if (stream.Length > MaxLogfileSize)
                //    return HttpResponse.PlainText($"That logfile is too large at {stream.Length} bytes. Maximum allowed size is {MaxLogfileSize} bytes.", HttpStatusCode._406_NotAcceptable);
                using var text = new StreamReader(stream);
                string line;
                while ((line = text.ReadLine()) != null)
                    if (line.StartsWith("[BombGenerator] Generating bomb with seed"))
                        goto good;
                return HttpResponse.PlainText("That’s not a valid KTANE logfile.", HttpStatusCode._406_NotAcceptable);
            }
            good:;

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
            var keywordsJson = keywords.Select(k => $@"{k.JsEscape()}:").ToArray();

            static bool containsModules(IEnumerable<string> logfileLines, string[] moduleIdsJson)
            {
                var needs = moduleIdsJson.ToList();
                using var e = logfileLines.GetEnumerator();
                while (e.MoveNext())
                {
                    var line = e.Current;
                    const string str = @"[Tweaks] LFABombInfo ";
                    if (!line.StartsWith(str) || !int.TryParse(line.Substring(str.Length), out var num))
                        continue;
                    var jsonCode = new StringBuilder();
                    for (var i = 0; i < num; i++)
                    {
                        if (!e.MoveNext())
                            break;
                        jsonCode.Append(e.Current);
                    }
                    var jsonCodeStr = jsonCode.ToString();
                    needs.RemoveAll(jsonCodeStr.Contains);
                    if (needs.Count == 0)
                        return true;
                }
                return false;
            }

            var list = new DirectoryInfo(_config.LogfilesDir)
                .EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly)
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .Take(1000)
                .Where(fi => containsModules(File.ReadLines(fi.FullName), keywordsJson))
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

        private FileSystemHandler _logfileFsHandler = null; // Assigned in Init()

        private HttpResponse logFileHandler(HttpRequest req)
        {
            if (string.IsNullOrWhiteSpace(_config.Archive7zPath))
                return _logfileFsHandler.Handle(req);

            var m = Regex.Match(req.Url.Path, @"^/([0-9a-f]{40}\.txt)$");
            if (!m.Success)
                return _logfileFsHandler.Handle(req);

            var filename = m.Groups[1].Value;
            if (File.Exists(Path.Combine(_config.LogfilesDir, filename)))
                return _logfileFsHandler.Handle(req);

            // Check if the requested logfile is in the relevant archive
            var archivePath = Path.Combine(_config.LogfilesDir, $"Ktane Logfiles {filename.Substring(0, 2)}.7z");
            if (!File.Exists(archivePath))
                return _logfileFsHandler.Handle(req);

            CommandRunner.Run(_config.Archive7zPath, "e", archivePath, filename).WithWorkingDirectory(_config.LogfilesDir).OutputAugmented().Go();

            // Regardless of whether the file was successfully extracted or not
            return _logfileFsHandler.Handle(req);
        }
    }
}
