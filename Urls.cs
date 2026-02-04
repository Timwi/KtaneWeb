using System;
using System.Collections.Generic;
using RT.Servers;
using RT.Util;

namespace KtaneWeb
{
    partial class KtanePropellerModule
    {
        private UrlResolver _urlResolver;

        private void InitUrlResolver()
        {
            var debug = false;
#if DEBUG
            debug = true;
#endif

            _urlResolver = new UrlResolver(
                new UrlMapping(path: "/js", specificPath: true, handler: req => debug ? HttpResponse.File(_config.JavaScriptFile, "text/javascript; charset=utf-8") : HttpResponse.JavaScript(Resources.Js)),
                new UrlMapping(path: "/css", specificPath: true, handler: req => debug ? HttpResponse.File(_config.CssFile, "text/css; charset=utf-8") : HttpResponse.Css(Resources.Css)),

                new UrlMapping(path: "/", specificPath: true, handler: mainPage),
                new UrlMapping(path: "/profile", handler: generateProfileZip),
                new UrlMapping(path: "/json", handler: req =>
                {
                    if (req.Url.Path == "/raw")
                        return HttpResponse.Json(_moduleInfoCache.ModulesJson, HttpStatusCode._200_OK, new HttpResponseHeaders { AccessControlAllowOrigin = "*" });
                    else if (req.Url.Path == "/flavourtext")
                        return HttpResponse.Json(_moduleInfoCache.ModulesJsonFlavourText, HttpStatusCode._200_OK, new HttpResponseHeaders { AccessControlAllowOrigin = "*" });
                    else if (req.Url.Path == "/startingline")
                        return HttpResponse.Json(_moduleInfoCache.ModulesJsonStartingLine, HttpStatusCode._200_OK, new HttpResponseHeaders { AccessControlAllowOrigin = "*" });
                    else
                        return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/JSON" + req.Url.Path));
                }),
                new UrlMapping(path: "/pull", handler: pull),
                new UrlMapping(path: "/ManualLastUpdated", handler: ManualLastUpdated),
                new UrlMapping(path: "/proxy", handler: proxy),
                new UrlMapping(path: "/merge-pdf", handler: mergePdfs),
                new UrlMapping(path: "/pdf-diag", handler: pdfDiag),
                new UrlMapping(path: "/upload-log", handler: uploadLogfile),
                new UrlMapping(path: "/find-log", handler: findLogfile),
                new UrlMapping(path: "/generate-json", handler: generateJson),
                new UrlMapping(path: "/iconsprite", handler: req => HttpResponse.Create(_moduleInfoCache.IconSpritePng, "image/png")),
                new UrlMapping(path: "/sitemap", specificPath: true, handler: sitemapXml),
                new UrlMapping(path: "/redirect", specificPath: false, handler: req => debug ? HttpResponse.File(_config.RedirectHtmlFile, "text/html; charset=utf-8") : HttpResponse.Html(Resources.RedirectHtml)),

                new UrlMapping(path: "/puzzles", handler: req => new KtaneWebSession(_config).EnableAutomatic(req, session => puzzles(req, _config.Puzzles, session))),

                new UrlMapping(path: "/Unfinished", handler: unfinished, skippable: true),
                new UrlMapping(path: "/MergedPdfs", handler: new FileSystemHandler(_config.MergedPdfsDir).Handle),
                new UrlMapping(path: "/Logfiles", handler: logFileHandler),

                // Shortcut URLs
                new UrlMapping(path: "/lfa", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/Logfile Analyzer.html"))),
                new UrlMapping(path: "/faq", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/Glossary.html"))),
                new UrlMapping(path: "/pe", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/Profile Editor.html"))),
                new UrlMapping(path: "/mse", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/Mode Settings Editor.html"))),

                // Redirects from old file names
                new UrlMapping(path: "/More/FAQs.html", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPath("/More/Glossary.html"))),

                // Default fallback: file system handler or PDF generator
                new UrlMapping(handler: req => pdf(req) ?? injectOpenGraphData(req, new FileSystemHandler(_config.BaseDir, new FileSystemOptions
                {
                    MimeTypeOverrides = new Dictionary<string, string>
                    {
                        ["wav"] = "audio/wav"
                    }
                }).Handle(req))),

                // Auth
                new UrlMapping(path: "/auth", handler: req =>
                {
                    var auth = _config.UsersFile?.Apply(file => new FileAuthenticator(file, _ => req.Url.WithPath("").ToHref(), "Repository of Manual Pages"));
                    return auth == null ? null : new KtaneWebSession(_config).EnableAutomatic(req, session => auth.Handle(req, session.Username, user =>
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
                    }));
                }),

                new UrlMapping(path: "/api", handler: API)

                );
        }
    }
}
