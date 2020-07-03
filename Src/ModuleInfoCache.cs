﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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
            public byte[] IconSpritePng;
            public string IconSpriteMd5;
            public string ModuleInfoJs;
            public DateTime LastModifiedUtc;
            public readonly Dictionary<string, string> ManualsLastModified = new Dictionary<string, string>();
        }
        private ModuleInfoCache _moduleInfoCache = null;

        private ModuleInfoCache getModuleInfoCache()
        {
            ModuleInfoCache mic = null;
            do
            {
                if (_moduleInfoCache == null)
                    lock (this)
                        if (_moduleInfoCache == null)
                        {
                            const int cols = 20;   // number of icons per row
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

                            // This needs to be a separate variable (don’t use _moduleInfoCache itself) because that field needs to stay null until it is fully initialized
                            var moduleInfoCache = new ModuleInfoCache { IconSpritePng = mem.ToArray() };
                            moduleInfoCache.IconSpriteMd5 = MD5.Create().ComputeHash(moduleInfoCache.IconSpritePng).ToHex();

                            // Load TP data from the spreadsheet
                            JsonList entries;
                            try
                            {
                                entries = new HClient().Get("https://spreadsheets.google.com/feeds/list/1G6hZW0RibjW7n72AkXZgDTHZ-LKj0usRkbAwxSPhcqA/1/public/values?alt=json").DataJson["feed"]["entry"].GetList();
                            }
                            catch (Exception e)
                            {
                                _logger.Exception(e);
                                entries = new JsonList();
                            }

                            var modules = new DirectoryInfo(Path.Combine(_config.BaseDir, "JSON"))
                                .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
                                .ParallelSelect(4, file =>
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

                                        // Merge in TP data
                                        static string normalize(string value) => value.ToLowerInvariant().Replace('’', '\'');
                                        var entry = entries.FirstOrDefault(entry =>
                                        {
                                            string entryName = Regex.Replace(entry["gsx$modulename"]["$t"].GetString(), @" \((Solve|Time)\)", "");
                                            return normalize(entryName) == normalize(mod.DisplayName ?? mod.Name);
                                        });

                                        if (entry != null)
                                        {
                                            mergeTPData(mod, entry);
                                            modJson = (JsonDict) ClassifyJson.Serialize(mod);
                                        }

                                        // Some module names contain characters that can’t be used in filenames (e.g. “?”)
                                        var fileName = Path.GetFileNameWithoutExtension(file.Name);
                                        if (mod.Name != fileName)
                                            modJson["FileName"] = fileName;

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
                                        _logger.Exception(e);
                                        return null;
                                    }
                                })
                                .WhereNotNull()
                                .ToArray();
                            moduleInfoCache.Modules = modules.Select(m => m.mod).ToArray();
                            moduleInfoCache.ModulesJson = new JsonDict { { "KtaneModules", modules.Select(m => m.modJson).ToJsonList() } };
                            moduleInfoCache.LastModifiedUtc = modules.Max(m => m.LastWriteTimeUtc);

                            var modJsons = modules.Where(tup => tup.mod.TranslationOf == null).Select(tup =>
                            {
                                var (modJson, mod, _) = tup;
                                var fileName = modJson.ContainsKey("FileName") ? modJson["FileName"].GetString() : mod.Name;
                                modJson["Sheets"] = _config.EnumerateSheetUrls(fileName, modules.Select(m => m.mod.Name).Where(m => m.Length > mod.Name.Length && m.StartsWith(mod.Name)).ToArray());
                                var (x, y) = coords.Get(fileName, (x: 0, y: 0));
                                modJson["X"] = x;   // note how this gets set to 0,0 for icons that don’t exist, which are the coords for the blank icon
                                modJson["Y"] = y;
                                return modJson;
                            }).ToJsonList();

                            var iconDirs = Enumerable.Range(0, _config.DocumentDirs.Length).SelectMany(ix => new[] { _config.OriginalDocumentIcons[ix], _config.ExtraDocumentIcons[ix] }).ToJsonList();
                            var disps = _displays.Select(d => d.id).ToJsonList();
                            var filters = _filters.Select(f => f.ToJson()).ToJsonList();
                            var selectables = _selectables.Select(sel => sel.ToJson()).ToJsonList();
                            var souvenir = EnumStrong.GetValues<KtaneModuleSouvenir>().ToJsonDict(val => val.ToString(), val => val.GetCustomAttribute<KtaneSouvenirInfoAttribute>().Apply(attr => new JsonDict { { "Tooltip", attr.Tooltip }, { "Char", attr.Char.ToString() } }));
                            moduleInfoCache.ModuleInfoJs = $@"initializePage({modJsons},{iconDirs},{_config.DocumentDirs.ToJsonList()},{disps},{filters},{selectables},{souvenir},'{moduleInfoCache.IconSpriteMd5}');";
                            _moduleInfoCache = moduleInfoCache;
                        }
                mic = _moduleInfoCache;
            }
            while (mic == null);
            return mic;
        }

        private void mergeTPData(KtaneModuleInfo mod, JsonValue entry)
        {
            string scoreString = entry["gsx$tpscore"]["$t"].GetString();
            if (string.IsNullOrEmpty(scoreString))
                return;

            var moduleName = entry["gsx$modulename"]["$t"].GetString();

            KtaneTwitchPlaysNeedyScoring? scoreMethod = null;
            if (moduleName.EndsWith(" (Solve)"))
                scoreMethod = KtaneTwitchPlaysNeedyScoring.Solves;
            else if (moduleName.EndsWith(" (Time)"))
                scoreMethod = KtaneTwitchPlaysNeedyScoring.Time;

            if (mod.TwitchPlays == null)
                mod.TwitchPlays = new KtaneTwitchPlaysInfo();

            var tp = mod.TwitchPlays;

            // The module has been determined, now parse the score.
            tp.NeedyScoring ??= scoreMethod;

            // UN and T is for unchanged and temporary score which are read normally.
            scoreString = Regex.Replace(scoreString, @"(?:UN )?(\d+)T?", "$1");

            // S is for special modules which we parse out the multiplier and put it into a dictionary and use later.
            var dynamicMatch = Regex.Match(scoreString, @"S ([\d.]+)x(?: \+ ([\d.]+))?");
            if (dynamicMatch.Success && decimal.TryParse(dynamicMatch.Groups[1].Value, out decimal dynamicScore))
            {
                // A + after S is for a static addition to the dynamic score.
                if (dynamicMatch.Groups.Count > 2)
                {
                    if (decimal.TryParse(dynamicMatch.Groups[2].Value, out decimal staticScore))
                        tp.Score ??= staticScore;
                }
                else
                {
                    tp.Score ??= 0;
                }

                tp.ScorePerModule ??= dynamicScore;
                return;
            }

            // PPA is for point per action modules which can be parsed in some cases.
            scoreString = Regex.Replace(scoreString, @"PPA ([\d.]+) \+ ([\d.]+)", "$2");

            // Catch any PPA or TDB modules which can't be parsed.
            if (scoreString.StartsWith("PPA ") || scoreString == "TBD")
                return;

            if (decimal.TryParse(scoreString, out decimal score))
                tp.Score ??= score;
        }
    }
}
