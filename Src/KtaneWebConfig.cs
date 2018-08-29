using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KtaneWeb.Puzzles;
using RT.Util;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    sealed class KtaneWebConfig : IClassifyJsonObjectProcessor
    {
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

        // User/password file for editing
        public string UsersFile;
        public string JavaScriptFile;
        public string CssFile;

        public string BaseDir = @"D:\Sites\KTANE\Public";
        public string[] DocumentDirs = new[] { "HTML", "PDF" };
        public string PdfDir = "PDF";
        public string[] OriginalDocumentIcons = new[] { "HTML/img/html_manual.png", "HTML/img/pdf_manual.png" };
        public string[] ExtraDocumentIcons = new[] { "HTML/img/html_manual_embellished.png", "HTML/img/pdf_manual_embellished.png" };
        public string ModIconDir = "Icons";

        public string PuzzlesJavaScriptFile;
        public string PuzzlesCssFile;

        public string VanillaRuleModifierCache;
        public string VanillaRuleModifierMods;

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value

        /// <summary>Keep the list sorted by date (most recent first).</summary>
        [ClassifyNotNull]
        public ListSorted<HistoryEntry<KtaneWebConfigEntry>> History = new ListSorted<HistoryEntry<KtaneWebConfigEntry>>(new CustomComparer<HistoryEntry<KtaneWebConfigEntry>>((a, b) => b.Time.CompareTo(a.Time)));

        [ClassifyNotNull]
        public ListSorted<HistoryEntry<KtaneWebConfigEntry>> HistoryDeleted = new ListSorted<HistoryEntry<KtaneWebConfigEntry>>(new CustomComparer<HistoryEntry<KtaneWebConfigEntry>>((a, b) => b.Time.CompareTo(a.Time)));

        public KtaneWebConfigEntry Current => History.Count == 0 ? null : History.First(h => !h.IsSuggestion).Entry;

        [ClassifyNotNull]
        public PuzzleInfo Puzzles = new PuzzleInfo();

        /// <summary>Maps from SessionID to Username.</summary>
        [ClassifyNotNull]
        public Dictionary<string, string> Sessions = new Dictionary<string, string>();

        public JsonList EnumerateSheetUrls(string moduleName, string[] notModuleNames)
        {
            if (moduleName == null)
                throw new ArgumentNullException(nameof(moduleName));

            var list = new List<string>();
            for (int i = 0; i < DocumentDirs.Length; i++)
            {
                var dirInfo = new DirectoryInfo(Path.Combine(BaseDir, DocumentDirs[i]));
                foreach (var inf in dirInfo.EnumerateFiles($"{moduleName}.*").Select(f => new { File = f, Icon = 2 * i }).Concat(dirInfo.EnumerateFiles($"{moduleName} *").Select(f => new { File = f, Icon = 2 * i + 1 })))
                    if (!notModuleNames.Any(inf.File.Name.StartsWith))
                        list.Add($"{Path.GetFileNameWithoutExtension(inf.File.Name).Substring(moduleName.Length)}|{inf.File.Extension.Substring(1)}|{inf.Icon}");
            }
            list.Sort();
            return list.ToJsonList();
        }

        void IClassifyObjectProcessor<JsonValue>.BeforeSerialize() { }
        void IClassifyObjectProcessor<JsonValue>.AfterSerialize(JsonValue element) { }

        void IClassifyObjectProcessor<JsonValue>.BeforeDeserialize(JsonValue element) { }
        void IClassifyObjectProcessor<JsonValue>.AfterDeserialize(JsonValue element)
        {
            while (HistoryDeleted.Count > 10)
                HistoryDeleted.RemoveAt(10);
        }
    }
}
