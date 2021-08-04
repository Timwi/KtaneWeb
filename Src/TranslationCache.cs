using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Json;
using RT.Serialization;
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
            var path = Path.Combine(_config.BaseDir, "Translations");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

#if DEBUG
            ClassifyJson.SerializeToFile(TranslationInfo.Default, Path.Combine(path, "en.json"));
#endif

            _translationCache = new DirectoryInfo(path)
                .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
                .ParallelSelect(Environment.ProcessorCount, file =>
                {
                    try
                    {
                        var translationJson = File.ReadAllText(file.FullName);
                        var translation = ClassifyJson.Deserialize<TranslationInfo>(JsonDict.Parse(translationJson));
                        translation.langCode = file.Name.Remove(file.Name.Length - 5);
                        var newJson = ClassifyJson.Serialize(translation);
                        translation.Json = newJson.ToString();

#if DEBUG
                        var newJsonIndented = newJson.ToStringIndented();
                        if (translationJson != newJsonIndented)
                            File.WriteAllText(file.FullName, newJsonIndented);
#endif

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
