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
        public override string Name => "Repository of Manual Pages for Keep Talking and Nobody Explodes";

        private KtaneWebConfig _config;
        private LoggerBase _logger;
        private UrlResolver _resolver;

        public override HttpResponse Handle(HttpRequest request)
        {
            var response = _resolver.Handle(request);
            if (response is HttpResponseContent h)
                h.UseGzip = UseGzipOption.DontUseGzip;
            return response;
        }

        public override void Init(LoggerBase log)
        {
            var original = File.ReadAllText(Settings.ConfigFile);
            _config = ClassifyJson.Deserialize<KtaneWebConfig>(JsonValue.Parse(original));
            var rewrite = serializeConfig();
            if (rewrite != original)
                File.WriteAllText(Settings.ConfigFile, rewrite);
            base.Init(log);
            _logger = log;
            VanillaRuleGenerator.Extensions.Debug.Logger = log;

            _resolver = new UrlResolver(
#if DEBUG
                new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.File(_config.JavaScriptFile, "text/javascript; charset=utf-8")),
                new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.File(_config.CssFile, "text/css; charset=utf-8")),
#else
                new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.JavaScript(Resources.Js)),
                new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.Css(Resources.Css)),
#endif

                new UrlMapping(path: "/", specificPath: true, handler: mainPage),
                new UrlMapping(path: "/profile", handler: generateProfile),
                new UrlMapping(path: "/json", handler: req => new KtaneWebSession(_config).EnableAutomatic(req, session => jsonPage(req, session))),
                new UrlMapping(path: "/pull", handler: pull),
                new UrlMapping(path: "/proxy", handler: proxy),
                new UrlMapping(path: "/manual", handler: manual),

                new UrlMapping(path: "/puzzles", handler: req => new KtaneWebSession(_config).EnableAutomatic(req, session => puzzles(req, _config.Puzzles, session))),

                // Default fallback: file system handler
                new UrlMapping(req => new FileSystemHandler(_config.BaseDir, new FileSystemOptions { MaxAge = null }).Handle(req))
            );

            foreach (string directory in Directory.GetDirectories(Path.Combine(_config.BaseDir, "HTML")))
                _resolver.Add(new UrlMapping(path: "/manual/" + Path.GetFileName(directory), handler: req => new FileSystemHandler(directory, new FileSystemOptions { MaxAge = null }).Handle(req)));

            var auth = _config.UsersFile?.Apply(file => new FileAuthenticator(file, url => url.WithPath("").ToHref(), "Repository of Manual Pages"));
            if (auth != null)
                _resolver.Add(new UrlMapping(path: "/auth", handler: req => new KtaneWebSession(_config).EnableAutomatic(req, session => auth.Handle(req, session.Username, user =>
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
                }))));
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
