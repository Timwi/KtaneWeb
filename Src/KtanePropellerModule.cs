using System;
using System.IO;
using System.Linq;
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

        private KtaneWebConfig _config;

        public override HttpResponse Handle(HttpRequest request)
        {
            var auth = _config.UsersFile?.Apply(file => new FileAuthenticator(file, _ => request.Url.WithPath("").ToHref(), "KTANE Web"));

            return new KtaneWebSession(_config).EnableAutomatic(request, session =>
            {
                var resolver = new UrlResolver(
#if DEBUG
                    new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.File(_config.Current.JavaScriptFile, "text/javascript; charset=utf-8")),
                    new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.File(_config.Current.CssFile, "text/css; charset=utf-8")),
#else
                    new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.JavaScript(Resources.Js)),
                    new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.Css(Resources.Css)),
#endif

                    new UrlMapping(path: "/", specificPath: true, handler: req => mainPage(req, _config.Current)),
                    new UrlMapping(path: "/json", handler: req => jsonPage(req, session)),

                    // Default fallback: file system handler
                    new UrlMapping(req => new FileSystemHandler(_config.Current.BaseDir, new FileSystemOptions { MaxAge = null }).Handle(req))
                );

                if (auth != null)
                    resolver.Add(new UrlMapping(path: "/auth", handler: req => auth.Handle(req, session.Username, user =>
                    {
                        session.Username = user;
                        lock (_config)
                        {
                            if (user == null)
                                _config.Sessions.Remove(session.SessionID);
                            else
                                _config.Sessions[session.SessionID] = user;
                            saveConfig();
                        }
                    })));

                return resolver.Handle(request);
            });
        }

        public override void Init(LoggerBase log)
        {
            var original = File.ReadAllText(Settings.ConfigFile);
            _config = ClassifyJson.Deserialize<KtaneWebConfig>(JsonValue.Parse(original));
            var rewrite = serializeConfig();
            if (rewrite != original)
                File.WriteAllText(Settings.ConfigFile, rewrite);
            base.Init(log);
        }

        private void saveConfig()
        {
            lock (_config)
                File.WriteAllText(Settings.ConfigFile, serializeConfig());
        }

        private static bool customComparison(object a, object b)
        {
            if (a is string || a is ValueType)
                return false;

            Array aa = a as Array, bb = b as Array;
            if (aa != null && bb != null)
                return aa.Length == bb.Length && Enumerable.Range(0, aa.Length).All(i => Equals(aa.GetValue(i), bb.GetValue(i)));

            return Equals(a, b);
        }

        private string serializeConfig()
        {
            return ClassifyJson.Serialize(_config, new ClassifyOptions { SerializationEqualityComparer = new CustomEqualityComparer<object>(customComparison) }).ToStringIndented();
        }
    }
}
