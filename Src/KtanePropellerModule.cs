using System;
using System.Collections.Generic;
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

        public override HttpResponse Handle(HttpRequest request) => new KtaneWebSession(_config).EnableAutomatic(request, session => _urlResolver.Handle(request));

        public override void Init()
        {
            Log.Info($"KtaneWeb configuration file: {Settings.ConfigFile}");

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

                    var appPath = PathUtil.AppPathCombine("..", "..");
                    config.JavaScriptFile = Path.Combine(appPath, "Src", "Resources", "KtaneWeb.js");
                    config.CssFile = Path.Combine(appPath, "Src", "Resources", "KtaneWeb.css");
                    if (!File.Exists(config.JavaScriptFile) || !File.Exists(config.CssFile))
                    {
                        Console.WriteLine();
                        tryAgain6:
                        ConsoleUtil.WriteLine("Finally, please let me know where you placed the KtaneWeb source code (what you’re running right now):".Color(ConsoleColor.Gray));
                        appPath = Console.ReadLine();
                        config.JavaScriptFile = Path.Combine(appPath, "Src", "Resources", "KtaneWeb.js");
                        config.CssFile = Path.Combine(appPath, "Src", "Resources", "KtaneWeb.css");
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

            if (!string.IsNullOrWhiteSpace(_config.LogfilesDir))
                Directory.CreateDirectory(_config.LogfilesDir);
            if (!string.IsNullOrWhiteSpace(_config.PdfTempPath))
                Directory.CreateDirectory(_config.PdfTempPath);
            if (!string.IsNullOrWhiteSpace(_config.MergedPdfsDir))
                Directory.CreateDirectory(_config.MergedPdfsDir);

            _logfileFsHandler = new FileSystemHandler(_config.LogfilesDir, new FileSystemOptions { ResponseHeaderProcessor = (h, t) => h.AccessControlAllowOrigin = "*" });

            generateTranslationCache();
            generateModuleInfoCache();
            InitUrlResolver();
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
