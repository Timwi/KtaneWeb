using System;
using System.IO;
using System.Linq;
using RT.Json;
using RT.PropellerApi;
using RT.Serialization;
using RT.Servers;
using RT.Util;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

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
                    new UrlMapping(path: "/profile", handler: generateProfileZip),
                    new UrlMapping(path: "/json", handler: req =>
                    {
                        if (req.Url.Path != "/raw")
                            return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/JSON/" + req.Url.Path));
                        return HttpResponse.Json(getModuleInfoCache().ModulesJson, HttpStatusCode._200_OK, new HttpResponseHeaders { AccessControlAllowOrigin = "*" });
                    }),
                    new UrlMapping(path: "/pull", handler: pull),
                    new UrlMapping(path: "/ManualLastUpdated", handler: ManualLastUpdated),
                    new UrlMapping(path: "/proxy", handler: proxy),
                    new UrlMapping(path: "/merge-pdf", handler: mergePdfs),
                    new UrlMapping(path: "/upload-log", handler: uploadLogfile),
                    new UrlMapping(path: "/find-log", handler: findLogfile),
                    new UrlMapping(path: "/generate-json", handler: generateJson),
                    new UrlMapping(path: "/iconsprite", handler: req => HttpResponse.Create(getModuleInfoCache().IconSpritePng, "image/png")),
                    new UrlMapping(path: "/sitemap", specificPath: true, handler: sitemapXml),

                    new UrlMapping(path: "/puzzles", handler: req => puzzles(req, _config.Puzzles, session)),

                    new UrlMapping(path: "/Unfinished", handler: unfinished, skippable: true),
                    new UrlMapping(path: "/Logfiles", handler: req => new FileSystemHandler(_config.LogfilesDir).Handle(req)),
                    new UrlMapping(path: "/MergedPdfs", handler: req => new FileSystemHandler(_config.MergedPdfsDir).Handle(req)),

                    // Shortcut URLs
                    new UrlMapping(path: "/lfa", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/Logfile Analyzer.html"))),
                    new UrlMapping(path: "/faq", handler: req => HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly("/More/FAQs.html"))),

                    // Default fallback: file system handler or PDF generator
                    new UrlMapping(handler: pdfOrFileSystem)
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
#if DEBUG
            if (string.IsNullOrWhiteSpace(Settings.ConfigFile))
            {
                var config = new KtaneWebConfig();
                Console.WriteLine();
                ConsoleUtil.WriteLine("It appears that you are running KtaneWeb for the first time.".Color(ConsoleColor.White));
                tryAgain1:
                ConsoleUtil.WriteLine(@"Please provide a location for the JSON settings file (for example: {0/DarkCyan}):".Color(ConsoleColor.Gray).Fmt(@"C:\Path\KtaneWeb.settings.json"));
                var path = Console.ReadLine();
                try
                {
                    ClassifyJson.SerializeToFile(config, path);
                }
                catch (Exception e)
                {
                    ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {e.Message.Color(ConsoleColor.Red)} {$"({e.GetType().FullName})".Color(ConsoleColor.DarkRed)}", null);
                    goto tryAgain1;
                }

                Console.WriteLine();
                tryAgain2:
                ConsoleUtil.WriteLine("Do you already have a local clone of the KtaneContent repository that you want the website to use?".Color(ConsoleColor.White));
                Console.WriteLine("If yes, please type the full path to that repository. If no, just press Enter.");
                var ktaneContent = Console.ReadLine();
                var expectedSubfolders = "HTML,More,JSON,Icons".Split(',');
                if (string.IsNullOrWhiteSpace(ktaneContent))
                {
                    ConsoleUtil.WriteLine(@"In that case we will create a new clone. I can do that automatically if you have git installed (if you don’t, please abort now).".Color(ConsoleColor.White));
                    ConsoleUtil.WriteLine("This will take a long time as the repository is large.".Color(ConsoleColor.White));
                    Console.WriteLine();
                    tryAgain3:
                    ConsoleUtil.WriteLine("Please choose a path where you would like all the data stored (for example: {0/DarkCyan}):".Color(ConsoleColor.Gray).Fmt(@"C:\Path\KtaneContent"));
                    var cloneFolder = Console.ReadLine();
                    try
                    {
                        Directory.CreateDirectory(cloneFolder);
                    }
                    catch (Exception e)
                    {
                        ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {e.Message.Color(ConsoleColor.Red)} {$"({e.GetType().FullName})".Color(ConsoleColor.DarkRed)}", null);
                        goto tryAgain3;
                    }
                    try
                    {
                        config.BaseDir = Path.Combine(cloneFolder, "Public");
                        CommandRunner.Run("git", "clone", "https://github.com/Timwi/KtaneContent.git", config.BaseDir).Go();
                        config.MergedPdfsDir = Path.Combine(cloneFolder, "MergedPdfs");
                        Directory.CreateDirectory(config.MergedPdfsDir);
                        config.LogfilesDir = Path.Combine(cloneFolder, "Logfiles");
                        Directory.CreateDirectory(config.LogfilesDir);
                    }
                    catch (Exception e)
                    {
                        ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {e.Message.Color(ConsoleColor.Red)} {$"({e.GetType().FullName})".Color(ConsoleColor.DarkRed)}", null);
                        goto tryAgain2;
                    }
                }
                else if (expectedSubfolders.Any(s => !Directory.Exists(Path.Combine(ktaneContent, s))))
                {
                    ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {"That folder does not appear to contain KtaneContent.".Color(ConsoleColor.Red)}", null);
                    ConsoleUtil.WriteLine("(We’re looking for a folder that contains subfolders named: {0/DarkMagenta})".Color(ConsoleColor.Magenta).Fmt(expectedSubfolders.JoinString(", ")));
                    goto tryAgain2;
                }
                else
                {
                    var p = ktaneContent;
                    while (p.EndsWith("\""))
                        p = Path.GetDirectoryName(p);
                    config.BaseDir = p;
                    p = Path.GetDirectoryName(p);

                    Console.WriteLine();
                    tryAgain4:
                    var logfiles = Path.Combine(p, "Logfiles");
                    ConsoleUtil.WriteLine("Please choose a path where you would like KtaneWeb to store logfiles uploaded through the Logfile Analyzer, or just press Enter to use the default ({0/DarkCyan}):".Color(ConsoleColor.Gray).Fmt(logfiles));
                    config.LogfilesDir = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(config.LogfilesDir))
                    {
                        ConsoleUtil.WriteLine("Using default: {0/DarkCyan}".Color(ConsoleColor.Gray).Fmt(logfiles));
                        config.LogfilesDir = logfiles;
                    }
                    try
                    {
                        Directory.CreateDirectory(config.LogfilesDir);
                    }
                    catch (Exception e)
                    {
                        ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {e.Message.Color(ConsoleColor.Red)} {$"({e.GetType().FullName})".Color(ConsoleColor.DarkRed)}", null);
                        goto tryAgain4;
                    }

                    Console.WriteLine();
                    tryAgain5:
                    var mergedPdfs = Path.Combine(p, "MergedPdfs");
                    ConsoleUtil.WriteLine("Please choose a path where you would like KtaneWeb to store merged PDFs, or just press Enter to use the default ({0/DarkCyan}):".Color(ConsoleColor.Gray).Fmt(mergedPdfs));

                    config.MergedPdfsDir = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(config.MergedPdfsDir))
                    {
                        ConsoleUtil.WriteLine("Using default: {0/DarkCyan}".Color(ConsoleColor.Gray).Fmt(mergedPdfs));
                        config.MergedPdfsDir = mergedPdfs;
                    }
                    try
                    {
                        Directory.CreateDirectory(config.MergedPdfsDir);
                    }
                    catch (Exception e)
                    {
                        ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {e.Message.Color(ConsoleColor.Red)} {$"({e.GetType().FullName})".Color(ConsoleColor.DarkRed)}", null);
                        goto tryAgain5;
                    }

                    var appPath = PathUtil.AppPathCombine(@"..\..");
                    config.JavaScriptFile = Path.Combine(appPath, @"Src\Resources\KtaneWeb.js");
                    config.CssFile = Path.Combine(appPath, @"Src\Resources\KtaneWeb.css");
                    if (!File.Exists(config.JavaScriptFile) || !File.Exists(config.CssFile))
                    {
                        Console.WriteLine();
                        tryAgain6:
                        ConsoleUtil.WriteLine("Finally, please let me know where you placed the KtaneWeb source code (what you’re running right now):".Color(ConsoleColor.Gray));
                        appPath = Console.ReadLine();
                        config.JavaScriptFile = Path.Combine(appPath, @"Src\Resources\KtaneWeb.js");
                        config.CssFile = Path.Combine(appPath, @"Src\Resources\KtaneWeb.css");
                        if (!File.Exists(config.JavaScriptFile) || !File.Exists(config.CssFile))
                        {
                            ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {"That does not look like the KtaneWeb source code folder.".Color(ConsoleColor.Red)}", null);
                            goto tryAgain6;
                        }
                    }
                }

                try
                {
                    ClassifyJson.SerializeToFile(config, path);
                    Settings.ConfigFile = path;
                    SaveSettings();
                }
                catch (Exception e)
                {
                    ConsoleUtil.WriteLine($"{"Problem:".Color(ConsoleColor.Magenta)} {e.Message.Color(ConsoleColor.Red)} {$"({e.GetType().FullName})".Color(ConsoleColor.DarkRed)}", null);
                    goto tryAgain1;
                }

                Console.WriteLine();
                ConsoleUtil.WriteLine("That should be all set up for you now!".Color(ConsoleColor.Green));
                ConsoleUtil.WriteLine("Feel free to browse the settings file we just created if you’re curious.".Color(ConsoleColor.DarkGreen));
                ConsoleUtil.WriteLine(@"For automatic PDF generation, we are assuming that Google Chrome is at its default location; if not, please change it manually in the JSON file.".Color(ConsoleColor.DarkGreen));
                Console.WriteLine();
                Console.WriteLine();
            }
#endif
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
            return a is string || a is ValueType || a is KtaneSouvenirInfo
                ? false
                : a is Array aa && b is Array bb
                    ? aa.Length == bb.Length && Enumerable.Range(0, aa.Length).All(i => customComparison(aa.GetValue(i), bb.GetValue(i)))
                    : Equals(a, b);
        }

        private string serializeConfig()
        {
            return ClassifyJson.Serialize(_config, new ClassifyOptions { SerializationEqualityComparer = new CustomEqualityComparer<object>(customComparison) }).ToStringIndented();
        }
    }
}
