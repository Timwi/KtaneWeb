using System.IO;
using System.Security.Cryptography;
using RT.Servers;
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

            if (req.Post["noredirect"].Value == "1")
                return HttpResponse.PlainText(req.Url.WithPathParent().WithPathOnly($"/lfa#file={sha1}").ToFull());
            else
                return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly($"/More/Logfile%20Analyzer.html#file={sha1}"));
        }
    }
}
