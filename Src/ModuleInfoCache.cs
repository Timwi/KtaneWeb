﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
            public byte[] IconSpritePng;
            public string IconSpriteCss;
            public string ModuleInfoJs;
            public DateTime LastModifiedUtc;

            // Key is just the HTML filename (with extension)
            public readonly Dictionary<string, string> ManualsLastModified = new();
            public readonly Dictionary<string, string> AutogeneratedPdfs = new();
        }
        private ModuleInfoCache _moduleInfoCache;

        private string[] getFlavourTexts(string htmlFilename)
        {
            string htmlContent;
            try
            {
                htmlContent = File.ReadAllText(htmlFilename);
            }
            catch (FileNotFoundException)
            {
                return new string[] { "" };
            }
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            var flav = htmlDoc.DocumentNode.Descendants("p").Where(node =>
                node.GetAttributeValue("class", "").Contains("flavour-text")
            ).ToList();

            List<string> texts = new List<string>();

            foreach (var text in flav)
            {
                if (text.InnerHtml != null && text.InnerHtml.Length > 0)
                    texts.Add(Regex.Replace(text.InnerHtml, @"\s+", " ").Trim());
            }

            return texts.ToArray();
        }

        // This method is called in Init() (when the server is initialized) and in pull() (when the repo is updated due to a new git commit).
        private void generateModuleInfoCache()
        {
            var moduleInfoCache = new ModuleInfoCache();
            Dictionary<string, string>[] tpEntries = null, timeModeEntries = null;
            Dictionary<string, (int x, int y)> coords = null;
            var exceptions = new JsonList();
            JsonValue contactInfoJson = null;

            var tasks = Ut.NewArray<(string name, Action action)>(
                ("Generating icon sprite", () => (moduleInfoCache.IconSpritePng, moduleInfoCache.IconSpriteCss, coords) = GenerateIconSprite()),
                ("Retrieving TP data from Google Sheets", () => tpEntries = LoadTpDataFromGoogleSheets()),
                ("Retrieving Time Mode data from Google Sheets", () => timeModeEntries = LoadTimeModeDataFromGoogleSheets()),
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
                .ToArray();

            static string getFileName(JsonDict modJson, KtaneModuleInfo mod) => modJson.ContainsKey("FileName") ? modJson["FileName"].GetString() : mod.Name;

            var flavourTextList = new JsonList();

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

                static string normalize(string value) => value.ToLowerInvariant().Replace('’', '\'');

                // Sheets and iconsprite coordinates
                var fileName = getFileName(modJson, mod);
                if (mod.TranslationOf == null)
                    modJson["Sheets"] = _config.EnumerateSheetUrls(fileName, modules.Select(m => m.mod.Name).Where(m => m.Length > mod.Name.Length && m.StartsWith(mod.Name)).ToArray());
                else if (!coords.ContainsKey(fileName))
                {
                    var origModule = modules.FirstOrNull(module => module.mod.ModuleID == mod.TranslationOf);
                    if (origModule != null)
                        fileName = getFileName(origModule.Value.modJson, origModule.Value.mod);
                }
                var (x, y) = coords.Get(fileName, (x: 0, y: 0));
                modJson["X"] = x;   // note how this gets set to 0,0 for icons that don’t exist, which are the coords for the blank icon
                modJson["Y"] = y;

                //get flavour-text from HTML of original manual
                string htmlFilename = Path.Combine(_config.BaseDir, "HTML/" + fileName + ".html");
                string[] flavourTexts = getFlavourTexts(htmlFilename);
                flavourTextList.Add(new JsonDict() {
                    { "Name", mod.Name },
                    { "Flavour", flavourTexts },
                    { "SteamID", mod.SteamID },
                    { "ModuleID", mod.ModuleID }
                });

                try
                {
                    // Merge in Time Mode data
                    var timeModeEntry = timeModeEntries?.FirstOrDefault(entry => normalize(entry["modulename"]) == normalize(mod.DisplayName ?? mod.Name));
                    if (timeModeEntry != null)
                        mergeTimeModeData(mod, modJson, timeModeEntry);
                }
                catch (Exception e)
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

                try
                {
                    // Merge in TP data
                    var tpEntry = tpEntries?.FirstOrDefault(entry => normalize(entry["modulename"]) == normalize(mod.DisplayName ?? mod.Name));
                    if (tpEntry != null)
                        mergeTPData(mod, modJson, tpEntry["tpscore"]);
                }
                catch (Exception e)
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

            moduleInfoCache.Modules = modules.Select(m => m.mod).ToArray();
            moduleInfoCache.ModulesJson = new JsonDict { { "KtaneModules", modules.Select(m => m.modJson).ToJsonList() } };
            moduleInfoCache.ModulesJsonFlavourText = new JsonDict { { "KtaneModules", flavourTextList } };
            moduleInfoCache.LastModifiedUtc = modules.Max(m => m.LastWriteTimeUtc);

            var iconDirs = Enumerable.Range(0, _config.DocumentDirs.Length).SelectMany(ix => new[] { _config.OriginalDocumentIcons[ix], _config.ExtraDocumentIcons[ix] }).ToJsonList();
            var disps = TranslationInfo.Default.Displays.Select(d => d.id).ToJsonList();
            var filters = TranslationInfo.Default.Filters1.Concat(TranslationInfo.Default.Filters2).Select(f => f.ToJson()).ToJsonList();
            var selectables = TranslationInfo.Default.Selectables.Select(sel => sel.ToJson()).ToJsonList();
            var souvenir = EnumStrong.GetValues<KtaneModuleSouvenir>().ToJsonDict(val => val.ToString(), val => val.GetCustomAttribute<KtaneSouvenirInfoAttribute>().Apply(attr => new JsonDict { { "Tooltip", attr.Tooltip }, { "Char", attr.Char.ToString() } }));

            moduleInfoCache.ModuleInfoJs = $@"initializePage({modules.Where(m => m.mod.TranslationOf == null).Select(m => m.modJson).ToJsonList()},{iconDirs},{_config.DocumentDirs.ToJsonList()},{disps},{filters},{selectables},{souvenir},{exceptions},{contactInfoJson ?? new JsonDict()});";
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
            var response = new HClient().Get($"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:json").DataString;
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

        private Dictionary<string, string>[] LoadTimeModeDataFromGoogleSheets()
        {
            var attempts = 4;
            retry:
            Dictionary<string, string>[] timeModeEntries;
            try
            {
                Log.Info($"Loading Time Mode spreadsheet (attempt {5 - attempts}/5)");
                timeModeEntries = getJsonFromSheets("16lz2mCqRWxq__qnamgvlD0XwTuva4jIDW1VPWX49hzM").ToArray();
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

        private Dictionary<string, string>[] LoadTpDataFromGoogleSheets()
        {
            var attempts = 4;
            retry:
            Dictionary<string, string>[] tpEntries;
            try
            {
                Log.Info($"Loading TP spreadsheet (attempt {5 - attempts}/5)");
                tpEntries = getJsonFromSheets("1G6hZW0RibjW7n72AkXZgDTHZ-LKj0usRkbAwxSPhcqA").ToArray();
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

        private (byte[] iconSpritePng, string iconSpriteCss, Dictionary<string, (int x, int y)> coords) GenerateIconSprite()
        {
            const int cols = 40;   // number of icons per row
            const int w = 32;   // width of an icon in pixels
            const int h = 32;   // height of an icon in pixels

            var iconFiles = new DirectoryInfo(Path.Combine(_config.BaseDir, "Icons")).EnumerateFiles("*.png", SearchOption.TopDirectoryOnly).OrderBy(file => file.Name != "blank.png").ToArray();
            var rows = (iconFiles.Length + cols - 1) / cols;
            var coords = new Dictionary<string, (int x, int y)>();
            using var bmp = new Bitmap(w * cols, h * rows);
            using (var g = Graphics.FromImage(bmp))
            {
                for (int i = 0; i < iconFiles.Length; i++)
                {
                    using (var icon = new Bitmap(iconFiles[i].FullName))
                        g.DrawImage(icon, w * (i % cols), h * (i / cols));
                    coords.Add(Path.GetFileNameWithoutExtension(iconFiles[i].Name), (i % cols, i / cols));
                }
            }
            using var mem = new MemoryStream();
            bmp.Save(mem, ImageFormat.Png);

            var iconSpritePng = mem.ToArray();
            var iconSpriteCss = $".mod-icon{{background-image:url(data:image/png;base64,{Convert.ToBase64String(iconSpritePng)})}}";
            return (iconSpritePng, iconSpriteCss, coords);
        }

        private void mergeTPData(KtaneModuleInfo mod, JsonDict modJson, string scoreString)
        {
            // UN and T is for unchanged and temporary score which are read normally.
            scoreString = Regex.Replace(scoreString, @"(UN|(?<=\d)T)", "");

            modJson["TwitchPlays"] = new JsonDict();

            decimal score = 0;

            var parts = new List<string>();
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
                        parts.Add(number.Pluralize("base point"));
                        score += number;
                        break;

                    case 2 when split[0] == "T":
                        parts.Add(number.Pluralize("point") + " per second");
                        score += 20 * 60 * number;
                        break;

                    // D is for needy deactivations.
                    case 2 when split[0] == "D":
                        parts.Add(number.Pluralize("point") + " per deactivation");
                        score += 20 * 60 / 65 * number;
                        break;

                    // PPA is for point per action modules which can be parsed in some cases.
                    case 2 when split[0] == "PPA":
                        parts.Add(number.Pluralize("point") + " per action");
                        score += 10 * number;
                        break;

                    // S is for special modules which we parse out the multiplier and put it into a dictionary and use later.
                    case 2 when split[0] == "S":
                        parts.Add(number.Pluralize("point") + " per module");
                        score += 10 * number;
                        break;
                }
            }

            mod.TwitchPlaysScore = score;
            modJson["TwitchPlays"]["Score"] = score;

            modJson["TwitchPlays"]["ScoreStringDescription"] = parts.JoinString(" + ");
        }

        private void mergeTimeModeData(KtaneModuleInfo mod, JsonValue modJson, Dictionary<string, string> entry)
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
