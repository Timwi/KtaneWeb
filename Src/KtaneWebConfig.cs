using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    sealed class KtaneWebConfig
    {
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

        public string BaseDir = @"D:\Sites\KTANE\Public";
        public string[] DocumentDirs = new[] { "HTML", "PDF" };
        public string[] OriginalDocumentIcons = new[] { "HTML/img/html_manual.png", "HTML/img/pdf_manual.png" };
        public string[] ExtraDocumentIcons = new[] { "HTML/img/html_manual_embellished.png", "HTML/img/pdf_manual_embellished.png" };
        public string ModIconDir = "Icons";

        public string LogoUrl;
        public string SteamIconUrl;
        public string UnityIconUrl;
        public string TutorialVideoIconUrl;

        // User/password file for editing
        public string UsersFile;

        public string JavaScriptFile;
        public string CssFile;

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value

        [ClassifyNotNull]
        public KtaneModuleInfo[] KtaneModules = new KtaneModuleInfo[0];

        public JsonList EnumerateSheetUrls(string moduleName, string[] notModuleNames)
        {
            if (moduleName == null)
                throw new ArgumentNullException(nameof(moduleName));

            var list = new List<JsonDict>();
            for (int i = 0; i < DocumentDirs.Length; i++)
            {
                var dirInfo = new DirectoryInfo(Path.Combine(BaseDir, DocumentDirs[i]));
                foreach (var inf in dirInfo.EnumerateFiles($"{moduleName}.*").Select(f => new { File = f, Icon = OriginalDocumentIcons[i] }).Concat(dirInfo.EnumerateFiles($"{moduleName} *").Select(f => new { File = f, Icon = ExtraDocumentIcons[i] })))
                    if (!notModuleNames.Any(inf.File.Name.StartsWith))
                        list.Add(new JsonDict {
                            { "name", $"{Path.GetFileNameWithoutExtension(inf.File.Name)} ({inf.File.Extension.Substring(1).ToUpperInvariant()})" },
                            { "url", $"{DocumentDirs[i]}/{inf.File.Name.UrlEscape()}" },
                            { "icon", inf.Icon }
                        });
            }
            return list.OrderBy(item => item["name"].GetString()).ToJsonList();
        }
    }
}