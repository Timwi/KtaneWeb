using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KtaneWeb.Puzzles;
using RT.Serialization;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    sealed class KtaneWebConfig
    {
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

        // User/password file for editing
        public string UsersFile;
        public string JavaScriptFile;
        public string CssFile;
        public string RedirectHtmlFile;

        public string BaseDir = @"C:\Sites\KTANE\Public";
        public string[] DocumentDirs = ["HTML", "PDF"];
        public string PdfDir = "PDF";
        public string[] OriginalDocumentIcons = ["HTML/img/html_manual.png", "HTML/img/pdf_manual.png"];
        public string[] ExtraDocumentIcons = ["HTML/img/html_manual_embellished.png", "HTML/img/pdf_manual_embellished.png"];
        public string PdfTempPath = null;

        public string ChromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
        public string Archive7zPath = null;

        public string LogfilesDir = @"C:\Sites\KTANE\Logfiles";
        public string MergedPdfsDir = @"C:\Sites\KTANE\MergedPdfs";

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value

        [ClassifyNotNull]
        public PuzzleInfo Puzzles = new();

        /// <summary>Maps from SessionID to Username.</summary>
        [ClassifyNotNull]
        public Dictionary<string, string> Sessions = [];

        public List<(string sheetData, string fileName)> EnumerateSheetUrls(string moduleFileName, string[] notModuleNames)
        {
            if (moduleFileName == null)
                throw new ArgumentNullException(nameof(moduleFileName));

            var list = new HashSet<(string sheetData, string fileName)>();
            for (var i = 0; i < DocumentDirs.Length; i++)
            {
                var dirInfo = new DirectoryInfo(Path.Combine(BaseDir, DocumentDirs[i]));
                var ext = DocumentDirs[i].ToLowerInvariant();
                foreach (var inf in dirInfo.EnumerateFiles($"{moduleFileName}.{ext}").Select(f => new { File = f, Icon = 2 * i }).Concat(dirInfo.EnumerateFiles($"{moduleFileName} *.{ext}").Select(f => new { File = f, Icon = 2 * i + 1 })))
                    // Insist that the capitalization of the module name is exact
                    if (inf.File.Name.StartsWith(moduleFileName) && !notModuleNames.Any(inf.File.Name.StartsWith))
                    {
                        list.Add(($"{Path.GetFileNameWithoutExtension(inf.File.Name).Substring(moduleFileName.Length)}|{inf.File.Extension.Substring(1)}|{inf.Icon}",
                            Path.GetFileName(inf.File.Name)));

                        // If this is a non-interactive HTML file, offer a PDF file which will be auto-generated at runtime
                        if (ext == "html" && !inf.File.Name.Contains("interactive"))
                            list.Add(($"{Path.GetFileNameWithoutExtension(inf.File.Name).Substring(moduleFileName.Length)}|pdf|{inf.Icon + 2}",
                                null));

                        // If this is a PDF file, remove the auto-generated one that was added when we were looking at HTML
                        if (ext == "pdf")
                            list.Remove(($"{Path.GetFileNameWithoutExtension(inf.File.Name).Substring(moduleFileName.Length)}|pdf|{inf.Icon}", null));
                    }
            }
            return list.OrderBy(x => (x.sheetData.Substring(0, x.sheetData.IndexOf('|')), x.fileName)).ToList();
        }
    }
}
