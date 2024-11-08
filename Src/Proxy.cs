﻿using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private static readonly string[] _proxyAllowedUrlPrefixes = ["https://cdn.discordapp.com/", "https://hastebin.com/raw/", "https://ktane.w00ty.com/raw/", "https://ktane.onpointcoding.net/"];

        private HttpResponse proxy(HttpRequest req)
        {
            if (req.Url.Path.Length == 0)
                throw new HttpException(HttpStatusCode._404_NotFound);
            var url = req.Url.Path.Substring(1);
            if (!_proxyAllowedUrlPrefixes.Any(url.StartsWith))
                throw new HttpException(HttpStatusCode._403_Forbidden);
            try
            {
                return HttpResponse.PlainText(new HttpClient().GetAsync(url).Result.Content.ReadAsStringAsync().Result);
            }
            catch (Exception e)
            {
                var sb = new StringBuilder();
                var exc = e;
                while (exc != null)
                {
                    sb.AppendLine($"{e.Message} ({e.GetType().FullName})");
                    sb.AppendLine(exc.StackTrace.Indent(8));
                    sb.AppendLine();
                    exc = exc.InnerException;
                }
                return HttpResponse.PlainText(sb.ToString(), HttpStatusCode._503_ServiceUnavailable);
            }
        }
    }
}
