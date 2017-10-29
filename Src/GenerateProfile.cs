using System;
using System.Linq;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse generateProfile(HttpRequest req, KtaneWebConfigEntry config)
        {
            HttpResponse generateDefExp(HttpRequest rq, int operation, string name, Func<KtaneModuleInfo, KtaneModuleDifficulty, bool> filter)
            {
                var desired = EnumStrong.Parse<KtaneModuleDifficulty>(rq.Url.Path.Substring(1));
                return HttpResponse.Create(
                    new JsonDict { { "DisabledList", config.KtaneModules.Where(k => k.ModuleID != null && (k.Type == KtaneModuleType.Regular || k.Type == KtaneModuleType.Needy) && filter(k, desired)).Select(k => k.ModuleID).ToJsonList() }, { "Operation", operation } }.ToString(),
                    "application/octet-stream",
                    headers: new HttpResponseHeaders { ContentDisposition = new HttpContentDisposition { Mode = HttpContentDispositionMode.Attachment, Filename = name.Fmt(desired.ToReadable()) } }
                );
            }

            return new UrlResolver(
                new UrlMapping(path: "/defuser", handler: rq => generateDefExp(rq, 1, @"""Veto defuser {0}.json""", (k, d) => k.DefuserDifficulty == d)),
                new UrlMapping(path: "/expert", handler: rq => generateDefExp(rq, 0, @"""+ Expert {0}.json""", (k, d) => k.ExpertDifficulty != d))
            ).Handle(req);
        }
    }
}
