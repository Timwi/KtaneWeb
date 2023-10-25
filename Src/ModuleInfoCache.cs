﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using HtmlAgilityPack;
using RT.Json;
using RT.Serialization;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private sealed class ModuleInfoCache
        {
            public KtaneModuleInfo[] Modules;
            public JsonDict ModulesJson;
            public JsonDict ModulesJsonFlavourText;
            public JsonDict ModulesJsonStartingLine;
            public byte[] IconSpritePng;
            public string IconSpriteCss;
            public string ModuleInfoJs;
            public DateTime LastModifiedUtc;

            // Key is just the HTML filename (with extension)
            public readonly Dictionary<string, string> ManualsLastModified = new();
            public readonly Dictionary<string, string> AutogeneratedPdfs = new();
        }
        private ModuleInfoCache _moduleInfoCache;

        private (string[] flavourTexts, string startingLine) getManualTexts(string htmlFilename)
        {
            string htmlContent;
            try { htmlContent = File.ReadAllText(htmlFilename); }
            catch (FileNotFoundException) { return (new string[] { "" }, ""); }

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var flavourTexts = new HashSet<string>();
            string startingLine = null;

            foreach (var tag in htmlDoc.DocumentNode.Descendants())
            {
                if (tag.OriginalName != "p" && tag.OriginalName != "li")
                    continue;

                if (tag.GetAttributeValue("class", "").Contains("flavour-text"))
                    flavourTexts.Add(Regex.Replace(tag.InnerHtml, @"\s+", " ").Trim());
                else if (startingLine == null)
                {
                    var text = tag.InnerText;
                    if (!text.ContainsIgnoreCase(" Appendix ") && !text.Contains(" SECTION ") && !text.ContainsIgnoreCase(" appendices ") && !text.ContainsIgnoreCase("you are looking at a different") && !text.ContainsIgnoreCase("you are looking at the wrong"))
                        startingLine = Regex.Replace(text, @"\s+", " ").Trim();
                }
            }

            return (
                flavourTexts: flavourTexts.Where(ft => !string.IsNullOrWhiteSpace(ft)).ToArray(),
                startingLine: startingLine ?? "[Manual has no starting line]"
            );
        }

        // This method is called in Init() (when the server is initialized) and in pull() (when the repo is updated due to a new git commit).
        private void generateModuleInfoCache()
        {
            var moduleInfoCache = new ModuleInfoCache();
            Dictionary<string, Dictionary<string, string>> tpEntries = null, timeModeEntries = null;
            var exceptions = new JsonList();
            JsonValue restrictedManualJson = null;
            JsonValue contactInfoJson = null;

            // Icon sprite parameters
            const int cols = 47;   // number of icons per row
            const int w = 32;   // width of an icon in pixels
            const int h = 32;   // height of an icon in pixels
            var coords = new Dictionary<string, (int x, int y)>();

            var tasks = Ut.NewArray<(string name, Action action)>(
                ("Retrieving TP data from Google Sheets", () => tpEntries = LoadTpDataFromGoogleSheets()),
                ("Retrieving Time Mode data from Google Sheets", () => timeModeEntries = LoadTimeModeDataFromGoogleSheets()),
                ("Loading restricted manuals", () => restrictedManualJson = JsonValue.Parse(File.ReadAllText(Path.Combine(_config.BaseDir, "More/ChallengeBombRestrictedManuals.json")))),
                ("Loading contact info", () => contactInfoJson = JsonValue.Parse(File.ReadAllText(Path.Combine(_config.BaseDir, "ContactInfo.json")))));

            tasks.ParallelForEach(tup =>
            {
                try { tup.action(); }
                catch (Exception e)
                {
                    lock (exceptions)
                    {
                        Log.Error($"{tup.name} ERROR:");
                        Log.Exception(e);
                        exceptions.Add($"{tup.name} ERROR: {e.Message} ({e.GetType().FullName})");
                    }
                }
            });

            var flavourTextList = new JsonList();
            var startingLineList = new JsonList();

            var modules = new DirectoryInfo(Path.Combine(_config.BaseDir, "JSON"))
                .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
                .ParallelSelect(Environment.ProcessorCount, file =>
                {
                    try
                    {
                        var origFile = File.ReadAllText(file.FullName);
                        var modJson = JsonDict.Parse(origFile);
                        var mod = ClassifyJson.Deserialize<KtaneModuleInfo>(modJson);

#if DEBUG
                        var newJson = (JsonDict) ClassifyJson.Serialize(mod);
                        var newJsonStr = newJson.ToStringIndented();
                        if (newJsonStr != origFile)
                            File.WriteAllText(file.FullName, newJsonStr);
                        modJson = newJson;
#endif

                        // Some module names contain characters that can’t be used in filenames (e.g. “?”)
                        mod.FileName = Path.GetFileNameWithoutExtension(file.Name);
                        if (mod.Name != mod.FileName)
                            modJson["FileName"] = mod.FileName;

                        if (string.IsNullOrEmpty(mod.Author) && mod.Contributors != null)
                            modJson["Author"] = mod.Contributors.ToAuthorString();

                        // Save JSON size by only including the SortKey when it’s not obvious
                        if (modJson["SortKey"].GetStringSafe() == Regex.Replace(mod.Name.ToUpperInvariant(), "^THE ", "").Where(ch => (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')).JoinString())
                            modJson.Remove("SortKey");

                        // Get flavour text from HTML of original manual
                        var (flavours, starting) = getManualTexts(Path.Combine(_config.BaseDir, "HTML", Path.GetFileNameWithoutExtension(file.Name) + ".html"));
                        var flavourTexts = new JsonDict
                        {
                            ["Name"] = mod.Name,
                            ["Flavour"] = flavours,
                            ["SteamID"] = mod.SteamID,
                            ["ModuleID"] = mod.ModuleID
                        };
                        lock (flavourTextList)
                            flavourTextList.Add(flavourTexts);

                        // Get starting lines from HTML of original manual
                        var startingLines = new JsonDict
                        {
                            ["Name"] = mod.Name,
                            ["Line"] = starting
                        };
                        lock (startingLineList)
                            startingLineList.Add(startingLines);

                        try
                        {
                            // Merge in Time Mode data
                            var timeModeEntry = timeModeEntries?.Get(tpNormalize(mod.DisplayName ?? mod.Name), null);
                            if (timeModeEntry != null)
                                mergeTimeModeData(mod, modJson, timeModeEntry);
                        }
                        catch (Exception e)
                        {
                            lock (this)
                            {
#if DEBUG
                                Console.WriteLine(mod.FileName);
                                Console.WriteLine(e.Message);
                                Console.WriteLine(e.GetType().FullName);
                                Console.WriteLine(e.StackTrace);
#endif
                                Log.Exception(e);
                                exceptions.Add($"{mod.FileName} error reading Time Mode data: {e.Message}");
                            }
                        }

                        try
                        {
                            // Merge in TP data
                            var tpEntry = tpEntries?.Get(tpNormalize(mod.DisplayName ?? mod.Name), null);
                            if (tpEntry != null)
                                mergeTPData(mod, modJson, tpEntry["tpscore"]);
                        }
                        catch (Exception e)
                        {
                            lock (this)
                            {
#if DEBUG
                                Console.WriteLine(mod.FileName);
                                Console.WriteLine(e.Message);
                                Console.WriteLine(e.GetType().FullName);
                                Console.WriteLine(e.StackTrace);
#endif
                                Log.Exception(e);
                                exceptions.Add($"{mod.FileName} error reading TP score data: {e.Message}");
                            }
                        }

                        return (modJson, mod, file.LastWriteTimeUtc).Nullable();
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Console.WriteLine(file);
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.GetType().FullName);
                        Console.WriteLine(e.StackTrace);
#endif
                        Log.Exception(e);
                        exceptions.Add($"{file.Name} error: {e.Message}");
                        return null;
                    }
                })
                .WhereNotNull()
                .OrderBy(mod => mod.mod.Published).ThenBy(mod => mod.mod.TranslationOf != null)  // Sort for the icon sprite; for everything else, the sort order doesn’t matter
                .ToArray();

            static string getFileName(JsonDict modJson, KtaneModuleInfo mod) => modJson.ContainsKey("FileName") ? modJson["FileName"].GetString() : mod.Name;

            // For generating the icon sprite, start with a bitmap that is at least as big as we need (possibly bigger)
            var curX = 0;
            var curY = 0;
            using var iconSpriteBmp = new Bitmap(w * cols, h * ((modules.Length + cols - 1) / cols));
            using var iconSpriteGr = Graphics.FromImage(iconSpriteBmp);
            {
                // blank icon
                using (var icon = new Bitmap(Path.Combine(_config.BaseDir, "Icons", "blank.png")))
                    iconSpriteGr.DrawImage(icon, 0, 0);

                var uniqueSortKeys = new Dictionary<string, KtaneModuleInfo>();
                var uniqueSymbols = new Dictionary<string, KtaneModuleInfo>();
                foreach (var (modJson, mod, _) in modules)
                {
                    // Process ignore lists that contain special operators
                    if (mod.Ignore != null && mod.Ignore.Any(str => str.StartsWith("+")))
                    {
                        var processedIgnoreList = new List<string>();
                        foreach (var str in mod.Ignore)
                        {
                            if (str.StartsWith("+") && EnumStrong.TryParse<KtaneQuirk>(str.Substring(1), out var quirk))
                                processedIgnoreList.AddRange(modules.Where(tup => tup.mod.Quirks.HasFlag(quirk)).Select(tup => tup.mod.DisplayName ?? tup.mod.Name));
                            else if (str.StartsWith("-"))
                                processedIgnoreList.Remove(str.Substring(1));
                            else if (!str.StartsWith("+"))
                                processedIgnoreList.Add(str);
                        }
                        modJson["IgnoreProcessed"] = processedIgnoreList.ToJsonList();
                    }

                    if (uniqueSortKeys.ContainsKey(mod.SortKey))
                    {
                        if (mod.TranslationOf != uniqueSortKeys[mod.SortKey].ModuleID && uniqueSortKeys[mod.SortKey].TranslationOf != mod.ModuleID)
                        {
                            var msg = string.Format("Module: {0}\nDuplicate SortKey {1} with module: {2}", mod.Name, mod.SortKey, uniqueSortKeys[mod.SortKey].Name);
                            exceptions.Add(msg);
#if DEBUG
                            Console.WriteLine(msg);
#endif
                        }
                    }
                    else
                        uniqueSortKeys.Add(mod.SortKey, mod);

                    if (mod.Symbol is null)
                    {
#if DEBUG
                        if (mod.TranslationOf is null) Console.WriteLine("Module: {0} has no Symbol", mod.Name);
#endif
                    }
                    else if (uniqueSymbols.ContainsKey(mod.Symbol))
                    {
                        if (mod.TranslationOf != uniqueSymbols[mod.Symbol].ModuleID && uniqueSymbols[mod.Symbol].TranslationOf != mod.ModuleID)
                        {
                            var msg = string.Format("Module: {0}\nDuplicate Symbol {1} with module: {2}", mod.Name, mod.Symbol, uniqueSymbols[mod.Symbol].Name);
                            exceptions.Add(msg);
#if DEBUG
                            Console.WriteLine(msg);
#endif
                        }
                    }
                    else
                        uniqueSymbols.Add(mod.Symbol, mod);

                    // Sheets
                    var fileName = getFileName(modJson, mod);
                    if (mod.TranslationOf == null)
                        modJson["Sheets"] = _config.EnumerateSheetUrls(fileName, modules
                            .Select(m => m.mod.FileName ?? m.mod.Name)
                            .Where(m => m.Length > (mod.FileName ?? mod.Name).Length && m.StartsWith(mod.FileName ?? mod.Name))
                            .ToArray());

                    // Iconsprite
                    if (mod.TranslationOf != null)
                    {
                        var origModule = modules.FirstOrNull(module => module.mod.ModuleID == mod.TranslationOf);
                        if (origModule != null && coords.Get(getFileName(origModule.Value.modJson, origModule.Value.mod), null) is (int, int) c)
                            coords.Add(fileName, c);
                    }
                    else
                    {
                        var iconFilePath = Path.Combine(_config.BaseDir, "Icons", fileName + ".png");
                        if (File.Exists(iconFilePath))
                        {
                            curX++;
                            if (curX == cols)
                            {
                                curY++;
                                curX = 0;
                            }
                            using (var icon = new Bitmap(iconFilePath))
                                iconSpriteGr.DrawImage(icon, w * curX, h * curY);
                            coords.Add(fileName, (curX, curY));
                        }
                    }

                    var (x, y) = coords.Get(fileName, (x: 0, y: 0));
                    modJson["X"] = x;   // note how this gets set to 0,0 for icons that don’t exist, which are the coords for the blank icon
                    modJson["Y"] = y;
                }
            }

            // Now that we know how many icons are in the icon sprite, create a bitmap of the correct size
            using var iconSpriteBmp2 = new Bitmap(w * cols, h * (curX == 0 ? curY : curY + 1));
            using var iconSpriteGr2 = Graphics.FromImage(iconSpriteBmp2);
            iconSpriteGr2.DrawImage(iconSpriteBmp, 0, 0);
            using var mem = new MemoryStream();
            iconSpriteBmp2.Save(mem, ImageFormat.Png);
            moduleInfoCache.IconSpritePng = mem.ToArray();
            moduleInfoCache.IconSpriteCss = $".mod-icon{{background-image:url(data:image/png;base64,{Convert.ToBase64String(moduleInfoCache.IconSpritePng)})}}";

            moduleInfoCache.Modules = modules.Select(m => m.mod).ToArray();
            moduleInfoCache.ModulesJson = new JsonDict { { "KtaneModules", modules.Select(m => m.modJson).ToJsonList() } };
            moduleInfoCache.ModulesJsonFlavourText = new JsonDict { { "KtaneModules", flavourTextList } };
            moduleInfoCache.ModulesJsonStartingLine = new JsonDict { { "KtaneModules", startingLineList } };
            moduleInfoCache.LastModifiedUtc = modules.Max(m => m.LastWriteTimeUtc);

            var iconDirs = Enumerable.Range(0, _config.DocumentDirs.Length).SelectMany(ix => new[] { _config.OriginalDocumentIcons[ix], _config.ExtraDocumentIcons[ix] }).ToJsonList();
            var filters = TranslationInfo.Default.Filters1.Concat(TranslationInfo.Default.Filters2).Select(f => f.ToJson()).ToJsonList();
            var selectables = TranslationInfo.Default.Selectables.Select(sel => sel.ToJson()).ToJsonList();
            var souvenir = EnumStrong.GetValues<KtaneModuleSouvenir>().ToJsonDict(val => val.ToString(), val => val.GetCustomAttribute<KtaneSouvenirInfoAttribute>().Apply(attr => new JsonDict { { "Tooltip", attr.Tooltip }, { "Char", attr.Char.ToString() } }));

            moduleInfoCache.ModuleInfoJs = "initializePage(" +
                $"{modules.Where(m => m.mod.TranslationOf == null).Select(m => m.modJson).ToJsonList()}," +
                $"{iconDirs}," +
                $"{_config.DocumentDirs.ToJsonList()}," +
                $"{filters}," +
                $"{selectables}," +
                $"{souvenir}," +
                $"{exceptions}," +
                $"{restrictedManualJson ?? new JsonDict()}," +
                $"{contactInfoJson ?? new JsonDict()});";
            _moduleInfoCache = moduleInfoCache;
        }

#pragma warning disable CS0649
        private class SheetResponse
        {
            public Table table;

            public class Table
            {
                public Column[] cols;
                public Row[] rows;
                public class Column
                {
                    public string label;
                }

                public class Row
                {
                    public Value[] c;

                    public class Value
                    {
                        public string v;
                    }
                }
            }
        }
#pragma warning restore CS0649

        private IEnumerable<Dictionary<string, string>> getJsonFromSheets(string sheetId)
        {
            var response = new HttpClient().GetAsync($"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:json").Result.Content.ReadAsStringAsync().Result;
            var match = Regex.Match(response, @"google.visualization.Query.setResponse\((.+)\)").Groups[1].Value;

            var sheetResponse = ClassifyJson.Deserialize<SheetResponse>(match);
            if (sheetResponse == null)
                yield break;

            var table = sheetResponse.table;
            var columns = table.cols.Select(column => Regex.Replace(column.label.ToLowerInvariant(), "[^a-z]", "")).ToArray();

            foreach (var row in table.rows)
            {
                var dictionary = new Dictionary<string, string>();
                for (int i = 0; i < columns.Length; i++)
                {
                    dictionary[columns[i]] = row.c[i]?.v ?? "";
                }

                yield return dictionary;
            }
        }

        private static string tpNormalize(string value) => value.ToLowerInvariant().Replace('’', '\'');

        private Dictionary<string, Dictionary<string, string>> LoadTimeModeDataFromGoogleSheets()
        {
            var attempts = 4;
            retry:
            var timeModeEntries = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                Log.Info($"Loading Time Mode spreadsheet (attempt {5 - attempts}/5)");
                foreach (var entry in getJsonFromSheets("16lz2mCqRWxq__qnamgvlD0XwTuva4jIDW1VPWX49hzM"))
                    timeModeEntries[tpNormalize(entry["modulename"])] = entry;
                Log.Info($"Loading Time Mode spreadsheet: SUCCESS");
            }
            catch
            {
                if (attempts-- > 0)
                {
                    Thread.Sleep(700);
                    goto retry;
                }
                throw;
            }

            return timeModeEntries;
        }

        private Dictionary<string, Dictionary<string, string>> LoadTpDataFromGoogleSheets()
        {
            var attempts = 4;
            retry:
            var tpEntries = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                Log.Info($"Loading TP spreadsheet (attempt {5 - attempts}/5)");
                foreach (var entry in getJsonFromSheets("1G6hZW0RibjW7n72AkXZgDTHZ-LKj0usRkbAwxSPhcqA"))
                    tpEntries[tpNormalize(entry["modulename"])] = entry;
                Log.Info($"Loading TP spreadsheet: SUCCESS");
            }
            catch
            {
                if (attempts-- > 0)
                {
                    Thread.Sleep(700);
                    goto retry;
                }
                throw;
            }

            return tpEntries;
        }

        private static void mergeTPData(KtaneModuleInfo mod, JsonDict modJson, string scoreString)
        {
            // UN and T is for unchanged and temporary score which are read normally.
            scoreString = Regex.Replace(scoreString, @"UN|(?<=\d)T", "");

            decimal score = 0;

            foreach (var factor in scoreString.SplitNoEmpty("+"))
            {
                if (factor == "TBD")
                    continue;

                var split = factor.SplitNoEmpty(" ");
                if (!split.Length.IsBetween(1, 2))
                    continue;

                var numberString = split[split.Length - 1];
                if (numberString.EndsWith("x")) // To parse "5x" we need to remove the x.
                    numberString = numberString.Substring(0, numberString.Length - 1);

                if (!decimal.TryParse(numberString, out var number))
                    continue;

                // We assume a bomb with 10 modules, 20 minutes, 65 seconds between activations and 10 actions to calculate scores.
                switch (split.Length)
                {
                    case 1:
                        score += number;
                        break;

                    case 2 when split[0] == "T":
                        score += 20 * 60 * number;
                        break;

                    // D is for needy deactivations.
                    case 2 when split[0] == "D":
                        score += 20 * 60 / 65 * number;
                        break;

                    // PPA is for point per action modules which can be parsed in some cases.
                    case 2 when split[0] == "PPA":
                        score += 10 * number;
                        break;

                    // S is for special modules which we parse out the multiplier and put it into a dictionary and use later.
                    case 2 when split[0] == "S":
                        score += 10 * number;
                        break;
                }
            }

            mod.TwitchPlaysScore = score;
            modJson["TwitchPlays"] = new JsonDict
            {
                ["Score"] = score,
                ["ScoreString"] = scoreString.Trim().Replace(" ", "")
            };
        }

        private static void mergeTimeModeData(KtaneModuleInfo mod, JsonValue modJson, Dictionary<string, string> entry)
        {
            // Get score strings
            string scoreString = entry["resolvedscore"].Trim();
            if (string.IsNullOrEmpty(scoreString))
                scoreString = "10";
            string scorePerModuleString = entry["resolvedbosspointspermodule"] ?? "";

            if (mod.TimeMode == null)
                mod.TimeMode = new KtaneTimeModeInfo();

            var timeMode = mod.TimeMode;

            // Determine the score orign
            if (!string.IsNullOrEmpty(entry["assignedscore"]))
                timeMode.Origin = KtaneTimeModeOrigin.Assigned;
            else if (!string.IsNullOrEmpty(entry["communityscore"]))
                timeMode.Origin = KtaneTimeModeOrigin.Community;
            else if (!string.IsNullOrEmpty(entry["tpscore"].Trim()))
                timeMode.Origin = KtaneTimeModeOrigin.TwitchPlays;
            else
                timeMode.Origin = KtaneTimeModeOrigin.Unassigned;

            // Parse scores
            if (decimal.TryParse(scoreString, out decimal score))
                timeMode.Score ??= score;

            if (decimal.TryParse(scorePerModuleString, out decimal scorePerModule))
                timeMode.ScorePerModule ??= scorePerModule;

            modJson["TimeMode"] = ClassifyJson.Serialize(mod.TimeMode);
        }
    }
}
