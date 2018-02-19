using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KtaneWeb.Puzzles;
using RT.Servers;
using RT.TagSoup;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse puzzles(HttpRequest req, PuzzleInfo info, KtaneWebSession session)
        {
            var resolver = new UrlResolver(
#if DEBUG
                new UrlMapping(path: "/js", specificPath: true, handler: rq => HttpResponse.File(_config.PuzzlesJavaScriptFile, "text/javascript; charset=utf-8")),
                new UrlMapping(path: "/css", specificPath: true, handler: rq => HttpResponse.File(_config.PuzzlesCssFile, "text/css; charset=utf-8")),
#else
                new UrlMapping(path: "/js", specificPath: true, handler: rq => HttpResponse.JavaScript(Resources.PuzzlesJs)),
                new UrlMapping(path: "/css", specificPath: true, handler: rq => HttpResponse.Css(Resources.PuzzlesCss)),
#endif

                new UrlMapping(path: "/api", handler: rq => _ajax.Handle(rq, new Api(_config, session, saveConfig))),
                new UrlMapping(path: "/HTML", handler: rq => _ajax.Handle(rq, new Api(_config, session, saveConfig))),
                new UrlMapping(path: "", specificPath: true, handler: rq => HttpResponse.Redirect(rq.Url.WithPath("/"))),
                new UrlMapping(path: "/", specificPath: true, handler: rq => puzzlesMainPage(rq, info, session)),

                // Fallback: file system handler
                new UrlMapping(rq => new FileSystemHandler(Path.Combine(_config.BaseDir, "puzzles"), new FileSystemOptions
                {
                    MaxAge = null,
                    DirectoryListingAuth = r => info.EditAccess.Contains(session.Username) ? null : HttpResponse.Empty(HttpStatusCode._403_Forbidden)
                }).Handle(req))
            );
            return resolver.Handle(req);
        }

        private HttpResponse puzzlesMainPage(HttpRequest req, PuzzleInfo info, KtaneWebSession session)
        {
            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "Content-Type", content = "text/html; charset=utf-8" },
                    new TITLE("Puzzles"),
                    new SCRIPT { src = "../HTML/js/jquery.3.1.1.min.js" },
                    new SCRIPT { src = "js" },
                    new LINK { href = "css", rel = "stylesheet", type = "text/css" }
                ),
                new BODY(new DIV { id = "everything" }._(new Api(_config, session).RenderBody()))
            ));
        }

        private AjaxHandler<Api> _ajax = new AjaxHandler<Api>(
#if DEBUG
            AjaxHandlerOptions.PropagateExceptions
#else
            AjaxHandlerOptions.ReturnExceptionsWithMessages
#endif
        );
    }
}
