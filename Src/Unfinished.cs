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
            return Directory.Exists(Path.Combine(_config.BaseDir, "HTML", pieces[0]))
                ? HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/HTML/" + pieces[0] + path.Substring(1 + pieces[0].Length)))
                : null; // make use of skippable handler
        }
    }
}
