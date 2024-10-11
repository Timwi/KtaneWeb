using System.IO;
using KtaneWeb.Puzzles;
using RT.Servers;
using RT.TagSoup;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse puzzles(HttpRequest req, PuzzleInfo info, KtaneWebSession session)
        {
            var resolver = new UrlResolver(
#if DEBUG
                new UrlMapping(path: "/js", specificPath: true, handler: rq => HttpResponse.File(_config.Puzzles.JavaScriptFile, "text/javascript; charset=utf-8")),
                new UrlMapping(path: "/css", specificPath: true, handler: rq => HttpResponse.File(_config.Puzzles.CssFile, "text/css; charset=utf-8")),
#else
                new UrlMapping(path: "/js", specificPath: true, handler: rq => HttpResponse.JavaScript(Resources.PuzzlesJs)),
                new UrlMapping(path: "/css", specificPath: true, handler: rq => HttpResponse.Css(Resources.PuzzlesCss)),
#endif

                new UrlMapping(path: "/api", handler: rq => _puzzlesAjax.Handle(rq, new Api(_config, session, saveConfig))),
                new UrlMapping(path: "/HTML", handler: rq => _puzzlesAjax.Handle(rq, new Api(_config, session, saveConfig))),
                new UrlMapping(path: "", specificPath: true, handler: rq => HttpResponse.Redirect(rq.Url.WithPath("/"))),
                new UrlMapping(path: "/", specificPath: true, handler: rq => puzzlesMainPage(rq, info, session)),

                // Fallback: file system handler
                new UrlMapping(rq => new FileSystemHandler(Path.Combine(_config.BaseDir, "puzzles")).Handle(req))
            );
            return resolver.Handle(req);
        }

        private HttpResponse puzzlesMainPage(HttpRequest req, PuzzleInfo info, KtaneWebSession session)
        {
            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "Content-Type", content = "text/html; charset=utf-8" },
                    new TITLE("Puzzles"),
                    new SCRIPT { src = "js" },
                    new LINK { href = "css", rel = "stylesheet", type = "text/css" }
                ),
                new BODY(new DIV { id = "everything" }._(new Api(_config, session).RenderBody())),
                new SCRIPTLiteral("initializePuzzles();")
            ));
        }

        private readonly AjaxHandler<Api> _puzzlesAjax = new(
#if DEBUG
            AjaxHandlerOptions.PropagateExceptions
#else
            AjaxHandlerOptions.ReturnExceptionsWithMessages
#endif
        );
    }
}
