using System.IO;
using RT.PropellerApi;
using RT.Servers;
using RT.Util;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule : PropellerModuleBase<KtaneSettings>
    {
        public override string Name => "Keep Talking and Nobody Explodes — Mods and Modules";

        public override HttpResponse Handle(HttpRequest request)
        {
            var config = ClassifyJson.DeserializeFile<KtaneWebConfig>(Settings.ConfigFile);
            var auth = config.UsersFile?.Apply(file => new FileAuthenticator(file, _ => request.Url.WithPath("").ToHref(), "KTANE Web"));

            return Session.EnableManual<KtaneWebSession>(request, session =>
            {
                var resolver = new UrlResolver(
#if DEBUG
                    new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.File(config.JavaScriptFile, "text/javascript; charset=utf-8")),
                    new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.File(config.CssFile, "text/css; charset=utf-8")),
#else
                    new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.JavaScript(Resources.Js)),
                    new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.Css(Resources.Css)),
#endif

                    new UrlMapping(path: "/", specificPath: true, handler: req => mainPage(req, config)),
                    new UrlMapping(path: "/json", handler: req => jsonPage(req, config)),

                    // Default fallback: file system handler
                    new UrlMapping(req => new FileSystemHandler(config.BaseDir, new FileSystemOptions { MaxAge = null }).Handle(req))
                );

                if (auth != null)
                    resolver.Add(new UrlMapping(path: "/auth", handler: req => auth.Handle(req, session.Username, user => { session.Username = user; })));

                return resolver.Handle(request);
            });
        }

        public override void Init(LoggerBase log)
        {
            var original = File.ReadAllText(Settings.ConfigFile);
            var config = ClassifyJson.Deserialize<KtaneWebConfig>(JsonValue.Parse(original));
            var rewrite = ClassifyJson.Serialize(config).ToStringIndented();
            if (rewrite != original)
                File.WriteAllText(Settings.ConfigFile, rewrite);
            base.Init(log);
        }
    }
}
