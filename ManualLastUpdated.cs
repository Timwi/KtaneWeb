using System.IO;
using System.Linq;
using System.Text;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse ManualLastUpdated(HttpRequest req)
        {
            var filename = req.Url.Path.UrlUnescape();
            if (Path.GetInvalidPathChars().Any(ch => filename.Contains(ch)))
                return HttpResponse.PlainText($"“{filename}” contains a character not allowed in file names.", HttpStatusCode._400_BadRequest);

            var moduleInfoCache = _moduleInfoCache;

            lock (moduleInfoCache.ManualsLastModified)
            {
                if (!moduleInfoCache.ManualsLastModified.TryGetValue(filename, out var result))
                {
                    string htmlFile = new DirectoryInfo(Path.Combine(_config.BaseDir, "HTML")).GetFiles(Path.GetFileNameWithoutExtension(filename) + ".html").Select(fs => fs.FullName).FirstOrDefault();
                    if (htmlFile == null)
                        return HttpResponse.PlainText("Manual doesn’t exist.", HttpStatusCode._404_NotFound);

                    string relativeFilename = Path.GetFileName(htmlFile);
                    if (filename != "/" + relativeFilename)
                        return HttpResponse.Redirect(req.Url.WithPath("/" + relativeFilename));

                    var output = new StringBuilder();
                    var cmd = new CommandRunner
                    {
                        Command = $@"git log -n 1 --format=%cd ""HTML/{relativeFilename}""",
                        WorkingDirectory = _config.BaseDir
                    };
                    cmd.StdoutText += str => output.Append(str);
                    cmd.StderrText += str => output.Append(str);
                    cmd.StartAndWait();
                    result = moduleInfoCache.ManualsLastModified[filename] = output.ToString();
                }
                return HttpResponse.PlainText(result);
            }
        }
    }
}
