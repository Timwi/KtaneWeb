using System;
using System.Collections.Generic;
using System.Linq;
using RT.Serialization;
using RT.Util;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    sealed class KtaneWebConfigEntry : IEquatable<KtaneWebConfigEntry>
    {
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
