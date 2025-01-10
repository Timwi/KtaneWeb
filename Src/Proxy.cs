using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using RT.Json;
using RT.Servers;
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
            url = refreshDiscordAttachment(req.Url) ?? url;
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

        private string refreshDiscordAttachment(IHttpUrl fullUrl)
        {
            string url = fullUrl.Path.Substring(1) + fullUrl.QueryString;
            if (!url.StartsWith("https://cdn.discordapp.com/attachments/") || _config.DiscordBotToken == null)
                return null;

            var parsed = new HttpUrl("cdn.discordapp.com", url.Substring("https://cdn.discordapp.com".Length));
            var expired = !int.TryParse(parsed.QueryValues("ex").FirstOrDefault("0"), System.Globalization.NumberStyles.HexNumber, null, out int expirationSeconds) || expirationSeconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!expired)
                return url;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new("Bot", _config.DiscordBotToken);
            var response = client
                .PostAsync(
                    "https://discord.com/api/v10/attachments/refresh-urls",
                    new StringContent(
                        new JsonDict { ["attachment_urls"] = new JsonList { url } }.ToString(),
                        Encoding.UTF8,
                        "application/json"
                    )
                ).Result.Content.ReadAsStringAsync().Result;
            return JsonDict.Parse(response)["refreshed_urls"].GetList().First()["refreshed"].GetString();
        }
    }
}
