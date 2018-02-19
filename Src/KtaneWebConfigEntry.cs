using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Util;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    sealed class KtaneWebConfigEntry : IEquatable<KtaneWebConfigEntry>
    {
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

        [Obsolete]
        public string BaseDir = @"D:\Sites\KTANE\Public";
        [Obsolete]
        public string[] DocumentDirs = new[] { "HTML", "PDF" };
        [Obsolete]
        public string[] OriginalDocumentIcons = new[] { "HTML/img/html_manual.png", "HTML/img/pdf_manual.png" };
        [Obsolete]
        public string[] ExtraDocumentIcons = new[] { "HTML/img/html_manual_embellished.png", "HTML/img/pdf_manual_embellished.png" };
        [Obsolete]
        public string ModIconDir = "Icons";

        [Obsolete]
        public string JavaScriptFile;
        [Obsolete]
        public string CssFile;

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value

        [ClassifyNotNull]
        public ListSorted<KtaneModuleInfo> KtaneModules = new ListSorted<KtaneModuleInfo>(CustomComparer<KtaneModuleInfo>.By(mod => mod.SortKey));

        [ClassifyNotNull]
        public HashSet<string> AllowedEditors = new HashSet<string>();

        public bool Equals(KtaneWebConfigEntry other)
        {
            return other != null &&
                other.KtaneModules.SequenceEqual(KtaneModules) &&
                other.AllowedEditors.SequenceEqual(AllowedEditors);
        }

        public override int GetHashCode() => Ut.ArrayHash(Ut.ArrayHash(KtaneModules), Ut.ArrayHash(AllowedEditors));
        public override bool Equals(object obj) => Equals(obj as KtaneWebConfigEntry);
    }
}
