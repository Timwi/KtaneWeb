using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

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
        }
        private ModuleInfoCache _moduleInfoCache = null;

        private HttpResponse iconSpritePng(HttpRequest req)
        {
            ensureModuleInfoCache();
            return HttpResponse.Create(_moduleInfoCache.IconSpritePng, "image/png");
        }

        private void ensureModuleInfoCache()
        {
            if (_moduleInfoCache == null)
                lock (this)
                    if (_moduleInfoCache == null)
                    {
                        const int cols = 20;   // number of icons per row
                        const int w = 32;   // width of an icon in pixels
                        const int h = 32;   // height of an icon in pixels

                        var iconFiles = new DirectoryInfo(_config.ModIconDir).EnumerateFiles("*.png", SearchOption.TopDirectoryOnly).OrderBy(file => file.Name != "blank.png").ToArray();
                        var rows = (iconFiles.Length + cols - 1) / cols;
                        var coords = new Dictionary<string, (int x, int y)>();

                        using (var bmp = new Bitmap(w * cols, h * rows))
                        {
                            using (var g = Graphics.FromImage(bmp))
                            {
                                for (int i = 0; i < iconFiles.Length; i++)
                                {
                                    using (var icon = new Bitmap(iconFiles[i].FullName))
                                        g.DrawImage(icon, w * (i % cols), h * (i / cols));
                                    coords.Add(Path.GetFileNameWithoutExtension(iconFiles[i].Name), (i % cols, i / cols));
                                }
                            }
                            using (var mem = new MemoryStream())
                            {
                                bmp.Save(mem, ImageFormat.Png);
                                _moduleInfoCache = new ModuleInfoCache { IconSpritePng = mem.ToArray() };
                                _moduleInfoCache.IconSpriteMd5 = MD5.Create().ComputeHash(_moduleInfoCache.IconSpritePng).ToHex();

                                var modules = new DirectoryInfo(_config.ModJsonDir)
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
                                _moduleInfoCache.Modules = modules.Select(m => m.mod).ToArray();
                                _moduleInfoCache.ModulesJson = new JsonDict { { "KtaneModules", modules.Select(m => m.modJson).ToJsonList() } };
                                _moduleInfoCache.LastModifiedUtc = modules.Max(m => m.LastWriteTimeUtc);

                                var modJsons = modules.Where(tup => tup.mod.TranslationOf == null).Select(tup =>
                                {
                                    var (modJson, mod, _) = tup;
                                    modJson["Sheets"] = _config.EnumerateSheetUrls(mod.Name, modules.Select(m => m.mod.Name).Where(m => m.Length > mod.Name.Length && m.StartsWith(mod.Name)).ToArray());
                                    var (x, y) = coords.Get(mod.Name, (x: 0, y: 0));
                                    modJson["X"] = x;   // note how this gets set to 0,0 for icons that don’t exist, which are the coords for the blank icon
                                    modJson["Y"] = y;
                                    return modJson;
                                }).ToJsonList();

                                var iconDirs = Enumerable.Range(0, _config.DocumentDirs.Length).SelectMany(ix => new[] { _config.OriginalDocumentIcons[ix], _config.ExtraDocumentIcons[ix] }).ToJsonList();
                                var disps = _displays.Select(d => d.id).ToJsonList();
                                var filters = _filters.Select(f => f.ToJson()).ToJsonList();
                                var selectables = _selectables.Select(sel => sel.ToJson()).ToJsonList();
                                var souvenir = EnumStrong.GetValues<KtaneModuleSouvenir>().ToJsonDict(val => val.ToString(), val => val.GetCustomAttribute<KtaneSouvenirInfoAttribute>().Apply(attr => new JsonDict { { "Tooltip", attr.Tooltip }, { "Char", attr.Char.ToString() } }));
                                _moduleInfoCache.ModuleInfoJs = $@"initializePage({modJsons},{iconDirs},{_config.DocumentDirs.ToJsonList()},{disps},{filters},{selectables},{souvenir},'{_moduleInfoCache.IconSpriteMd5}');";
                            }
                        }
                    }
        }
    }
}
