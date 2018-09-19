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

        public override HttpResponse Handle(HttpRequest request)
        {
            var auth = _config.UsersFile?.Apply(file => new FileAuthenticator(file, _ => request.Url.WithPath("").ToHref(), "Repository of Manual Pages"));

            return new KtaneWebSession(_config).EnableAutomatic(request, session =>
            {
                var resolver = new UrlResolver(
#if DEBUG
                    new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.File(_config.JavaScriptFile, "text/javascript; charset=utf-8")),
                    new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.File(_config.CssFile, "text/css; charset=utf-8")),
#else
                    new UrlMapping(path: "/js", specificPath: true, handler: req => HttpResponse.JavaScript(Resources.Js)),
                    new UrlMapping(path: "/css", specificPath: true, handler: req => HttpResponse.Css(Resources.Css)),
#endif

                    new UrlMapping(path: "/", specificPath: true, handler: mainPage),
                    new UrlMapping(path: "/lfa", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/Logfile Analyzer.html"))),
                    new UrlMapping(path: "/profile", handler: generateProfile),
                    new UrlMapping(path: "/json", handler: req => jsonPage(req, session)),
                    new UrlMapping(path: "/pull", handler: pull),
                    new UrlMapping(path: "/proxy", handler: proxy),
                    new UrlMapping(path: "/merge-pdf", handler: pdf),
                    new UrlMapping(path: "/upload-log", handler: uploadLogfile),
                    new UrlMapping(path: "/find-log", handler: findLogfile),
                    new UrlMapping(path: "/puzzles", handler: req => puzzles(req, _config.Puzzles, session)),

                    new UrlMapping(path: "/Unfinished", handler: unfinished, skippable: true),
                    new UrlMapping(path: "/Logfiles", handler: req => new FileSystemHandler(_config.LogfilesDir).Handle(req)),
                    new UrlMapping(path: "/MergedPdfs", handler: req => new FileSystemHandler(_config.MergedPdfsDir).Handle(req)),

                    // Default fallback: file system handler
                    new UrlMapping(req => new FileSystemHandler(_config.BaseDir, new FileSystemOptions { MaxAge = null }).Handle(req))
                );

                foreach (string directory in Directory.GetDirectories(Path.Combine(_config.BaseDir, "HTML")))
                    resolver.Add(new UrlMapping(path: "/manual/" + Path.GetFileName(directory), handler: req => new FileSystemHandler(directory, new FileSystemOptions { MaxAge = null }).Handle(req)));

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
            _logger = log;
        }

        private void saveConfig()
        {
            lock (_config)
                File.WriteAllText(Settings.ConfigFile, serializeConfig());
        }

        private static bool customComparison(object a, object b)
        {
            if (a is string || a is ValueType || a is KtaneSouvenirInfo)
                return false;

            Array aa = a as Array, bb = b as Array;
            if (aa != null && bb != null)
                return aa.Length == bb.Length && Enumerable.Range(0, aa.Length).All(i => customComparison(aa.GetValue(i), bb.GetValue(i)));

            return Equals(a, b);
        }

        private string serializeConfig()
        {
            return ClassifyJson.Serialize(_config, new ClassifyOptions { SerializationEqualityComparer = new CustomEqualityComparer<object>(customComparison) }).ToStringIndented();
        }
    }
}
