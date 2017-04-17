using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Util;
using RT.Util.Collections;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    sealed class KtaneWebConfig : IClassifyJsonObjectProcessor
    {
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

        // User/password file for editing
        public string UsersFile;

#pragma warning restore 0649 // Field is never assigned to, and will always have its default value

        /// <summary>Keep the list sorted by date (most recent first).</summary>
        [ClassifyNotNull]
        public ListSorted<HistoryEntry<KtaneWebConfigEntry>> History = new ListSorted<HistoryEntry<KtaneWebConfigEntry>>(new CustomComparer<HistoryEntry<KtaneWebConfigEntry>>((a, b) => b.Time.CompareTo(a.Time)));

        [ClassifyNotNull]
        public ListSorted<HistoryEntry<KtaneWebConfigEntry>> HistoryDeleted = new ListSorted<HistoryEntry<KtaneWebConfigEntry>>(new CustomComparer<HistoryEntry<KtaneWebConfigEntry>>((a, b) => b.Time.CompareTo(a.Time)));

        public KtaneWebConfigEntry Current => History.Count == 0 ? null : History.First(h => !h.IsSuggestion).Entry;

        /// <summary>Maps from SessionID to Username.</summary>
        [ClassifyNotNull]
        public Dictionary<string, string> Sessions = new Dictionary<string, string>();

        void IClassifyObjectProcessor<JsonValue>.BeforeSerialize() { }
        void IClassifyObjectProcessor<JsonValue>.AfterSerialize(JsonValue element) { }

        void IClassifyObjectProcessor<JsonValue>.BeforeDeserialize(JsonValue element) { }
        void IClassifyObjectProcessor<JsonValue>.AfterDeserialize(JsonValue element)
        {
            if (element.ContainsKey("KtaneModules"))
            {
                History.Clear();
                History.Add(new HistoryEntry<KtaneWebConfigEntry>(DateTime.UtcNow, ClassifyJson.Deserialize<KtaneWebConfigEntry>(element), isSuggestion: false));
            }
        }
    }
}
