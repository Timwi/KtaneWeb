using System.IO;
using RT.Servers;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse unfinished(HttpRequest req)
        {
            var path = req.Url.Path;
            if (!path.StartsWith("/") || path == "/")
                return null;
            var pieces = path.Substring(1).Split('/');
            if (Directory.Exists(Path.Combine(_config.BaseDir, "HTML", pieces[0])))
                return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/HTML/" + pieces[0] + path.Substring(1 + pieces[0].Length)));
            return null;
        }
    }
}
