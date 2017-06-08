using System;
using RT.Servers;
using RT.Util;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse proxy(HttpRequest req)
        {
            var url = req.Url.Path.Substring(1);
            if (!url.StartsWith("https://cdn.discordapp.com/"))
                return HttpResponse.PlainText("", HttpStatusCode._403_Forbidden);
            try { return HttpResponse.PlainText(new HClient().Get(url).DataString); }
            catch (Exception e) { return HttpResponse.PlainText($"{e.Message} ({e.GetType().FullName})", HttpStatusCode._500_InternalServerError); }
        }
    }
}
