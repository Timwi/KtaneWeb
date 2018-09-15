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
            if (!req.FileUploads.TryGetValue("log", out var upload))
                return HttpResponse.PlainText("That’s not a valid KTANE logfile.", HttpStatusCode._406_NotAcceptable);

            using (var content = upload.GetStream())
            {
                var start = content.Read(128 * 1024).FromUtf8();
                if (!start.Contains("[BombGenerator] Generating bomb with seed"))
                    return HttpResponse.PlainText("That’s not a valid KTANE logfile.", HttpStatusCode._406_NotAcceptable);

                content.Seek(0, SeekOrigin.Begin);

                var sha1 = SHA1.Create().ComputeHash(content).ToHex();
                var filename = sha1 + ".txt";
                var path = Path.Combine(_config.LogfilesDir, filename);
                if (!File.Exists(path))
                    lock (this)
                        if (!File.Exists(path))
                            upload.SaveToFile(path);
                return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly($"/More/Logfile%20Analyzer.html#file={sha1}").ToHref());
            }
        }
    }
}
