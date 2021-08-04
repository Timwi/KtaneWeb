using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

        private Dictionary<string, TranslationInfo> _translationCache;

        // This method is called in Init() (when the server is initialized) and in pull() (when the repo is updated due to a new git commit).
        private void generateTranslationCache()
        {


            _translationCache =  new DirectoryInfo(Path.Combine(_config.BaseDir, "Translations"))
                .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
                .ParallelSelect(Environment.ProcessorCount, file =>
                {
                    try
                    {
                        var translationJson = File.ReadAllText(file.FullName);
                        var translation = ClassifyJson.Deserialize<TranslationInfo>(JsonDict.Parse(translationJson));
                        translation.langCode = file.Name.Remove(file.Name.Length - 5);
                        translation._json = ClassifyJson.Serialize(translation).ToString();
                        return translation;
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.GetType().FullName);
                        Console.WriteLine(e.StackTrace);
#endif
                        Log.Exception(e);
                        return null;
                    }
                }).ToDictionary(t => t.langCode, t => t);
        }
    }
}
