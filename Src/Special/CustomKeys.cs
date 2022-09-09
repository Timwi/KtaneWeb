using System;
using System.Linq;
using KtaneWeb.Special;
using RT.Servers;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse CustomKeys(HttpRequest req)
        {
            switch (req.Method)
            {
                case HttpMethod.Post:
                    {
                        if (!req.Post.ContainsKey("data") || req.Post["data"].Count < 1)
                            return HttpResponse.PlainText("Missing data.", HttpStatusCode._400_BadRequest);

                        return HttpResponse.Json(CustomKeysHolster.Push(req.Post["data"].Value));
                    }
                case HttpMethod.Get:
                    {
                        string[] codes = req.Url.QueryValues("code").ToArray();
                        if (codes.Length < 1)
                            return HttpResponse.PlainText("Missing code.", HttpStatusCode._400_BadRequest);

                        string code = codes.First();
                        if (!CustomKeysHolster.Has(code))
                            return HttpResponse.PlainText("Invalid code.", HttpStatusCode._404_NotFound);

                        return HttpResponse.PlainText(CustomKeysHolster.Pull(code));
                    }
                case HttpMethod.Delete:
                    {
                        string[] codes = req.Url.QueryValues("code").ToArray();
                        string[] tokens = req.Url.QueryValues("token").ToArray();
                        if (codes.Length < 1 || tokens.Length < 1)
                            return HttpResponse.PlainText("Missing code or token.", HttpStatusCode._400_BadRequest);

                        string code = codes.First();
                        if (!CustomKeysHolster.Has(code))
                            return HttpResponse.PlainText("Invalid code.", HttpStatusCode._404_NotFound);

                        if (!CustomKeysHolster.IsAuthorized(code, tokens.First()))
                            return HttpResponse.PlainText("Invalid token.", HttpStatusCode._401_Unauthorized);

                        CustomKeysHolster.Remove(code);

                        return HttpResponse.Empty();
                    }
                default:
                    return HttpResponse.PlainText("Invalid request method.", HttpStatusCode._405_MethodNotAllowed);
            }
        }
    }
}
