using System;
using System.IO;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        public HttpResponse JsonPage(HttpRequest req, KtaneWebConfig config)
        {
            return Session.EnableManual<KtaneWebSession>(req, session =>
            {
                var editable = req.Url.Path == "/editable";
                if (editable && session.Username == null)
                    return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/auth/login").WithQuery("returnto", req.Url.ToHref()));

                string error = null;

                if (req.Method == HttpMethod.Post && editable)
                {
                    var content = req.Post["json"].Value;
                    try
                    {
                        var newConfig = ClassifyJson.Deserialize<KtaneWebConfig>(JsonValue.Parse(content));
                        newConfig.KtaneModules = newConfig.KtaneModules.OrderBy(mod => mod.SortKey).ToArray();
                        var newJson = ClassifyJson.Serialize(newConfig);
                        File.WriteAllText(Settings.ConfigFile, newJson.ToStringIndented());
                    }
                    catch (Exception e)
                    {
                        error = $"{e.Message} ({e.GetType().Name})";
                    }
                }

                return HttpResponse.Html(new HTML(
                    new HEAD(
                        new TITLE("Keep Talking and Nobody Explodes — Mods and Modules — raw JSON data"),
                        new LINK { href = "//fonts.googleapis.com/css?family=Special+Elite", rel = "stylesheet", type = "text/css" },
                        new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                        new SCRIPT { src = "https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js" },
                        new META { name = "viewport", content = "width=device-width" }),
                    new BODY()._(
                        new H1("JSON"),
                        error?.Apply(_ => new DIV { class_ = "error" }._(error)),
                        new FORM { method = method.post, action = req.Url.ToHref() }._(
                            new TEXTAREA { name = "json", accesskey = "," }._(File.ReadAllText(Settings.ConfigFile)),
                            new DIV(
                                editable
                                    ? new BUTTON { type = btype.submit, accesskey = "s" }._("Save".Accel('S'))
                                    : new A { href = req.Url.WithPathOnly("/editable").ToHref(), accesskey = "e" }._("Edit".Accel('E')))))));
            });
        }
    }
}
