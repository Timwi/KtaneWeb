using System;
using System.Linq;
using RT.Servers;
using RT.Util;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private static string[] _proxyAllowedUrlPrefixes = new[] { "https://cdn.discordapp.com/", "https://hastebin.com/raw/", "https://ktane.w00ty.com/raw/" };

        private HttpResponse proxy(HttpRequest req)
        {
            if (req.Url.Path.Length == 0)
                throw new HttpException(HttpStatusCode._404_NotFound);
            var url = req.Url.Path.Substring(1);
            if (!_proxyAllowedUrlPrefixes.Any(url.StartsWith))
                throw new HttpException(HttpStatusCode._403_Forbidden);
            try { return HttpResponse.PlainText(new HClient().Get(url).DataString); }
            catch (Exception e) { return HttpResponse.PlainText($"{e.Message} ({e.GetType().FullName})", HttpStatusCode._503_ServiceUnavailable); }
        }
    }
}
