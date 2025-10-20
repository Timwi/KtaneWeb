using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse sitemapXml(HttpRequest req)
        {
            var moduleInfoCache = _moduleInfoCache;
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            static string d(DateTime dt) => dt.ToString("s", CultureInfo.InvariantCulture) + "Z";

            return HttpResponse.Create(contentType: "application/xml; charset=utf-8", content:
                new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    new XElement(ns + "urlset", new XAttribute(xsi + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),

                        // Main page
                        new XElement(ns + "url",
                            new XElement(ns + "loc", req.Url.WithPathParent().WithPathOnly($"/").ToFull()),
                            new XElement(ns + "lastmod", d(moduleInfoCache.LastModifiedUtc)),
                            new XElement(ns + "priority", "1")),

                        // HTML and PDF folders
                        _config.DocumentDirs
                            .Where(dir => Directory.Exists(Path.Combine(_config.BaseDir, dir)))
                            .Select(dir => new XElement(ns + "url",
                                new XElement(ns + "loc", req.Url.WithPathParent().WithPathOnly($"/{dir}/").ToFull()),
                                new XElement(ns + "lastmod", d(Directory.GetLastWriteTimeUtc(Path.Combine(_config.BaseDir, dir)))),
                                new XElement(ns + "priority", "1"))),

                        // HTML and PDF files
                        _config.DocumentDirs
                            .SelectMany(dir => new DirectoryInfo(Path.Combine(_config.BaseDir, dir)).Apply(dr => dr.EnumerateFiles("*.html").Concat(dr.EnumerateFiles("*.pdf")))
                                .Select(file => new XElement(ns + "url",
                                    new XElement(ns + "loc", req.Url.WithPathParent().WithPathOnly($"/{dir}/{file.Name.UrlEscape()}").ToFull()),
                                    new XElement(ns + "lastmod", d(file.LastWriteTimeUtc)),
                                    new XElement(ns + "priority", "0.8")))),

                        // Puzzles front page
                        new XElement(ns + "url",
                            new XElement(ns + "loc", req.Url.WithPathParent().WithPathOnly($"/puzzles/").ToFull()),
                            new XElement(ns + "lastmod", d(Directory.GetLastWriteTimeUtc(Path.Combine(_config.BaseDir, "puzzles")))),
                            new XElement(ns + "priority", "0.9")),

                        // Puzzles
                        _config.Puzzles.PuzzleGroups.Where(gr => gr.IsPublished).SelectMany(gr => gr.Puzzles
                            .Select(puz => (puzzle: puz, file: Path.Combine(_config.BaseDir, "puzzles", gr.Folder, puz.Filename)))
                            .Where(puz => puz.puzzle.IsPublished && File.Exists(puz.file))
                            .Select(puz => new XElement(ns + "url",
                                new XElement(ns + "loc", req.Url.WithPathParent().WithPathOnly($"/puzzles/{gr.Folder.UrlEscape()}/{puz.puzzle.Filename.UrlEscape()}").ToFull()),
                                new XElement(ns + "lastmod", d(File.GetLastWriteTimeUtc(puz.file))),
                                new XElement(ns + "priority", "0.6"))))
                    )
                ).ToString()
            );
        }
    }
}
