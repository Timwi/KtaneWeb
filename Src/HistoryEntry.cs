using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using RT.Util.Serialization;

namespace KtaneWeb
{
    sealed class HistoryEntry<T>
    {
        public DateTime Time { get; private set; }
        public T Entry { get; set; }

        [ClassifyIgnoreIfDefault]
        public bool IsSuggestion { get; set; }
        [ClassifyIgnoreIfDefault]
        public bool IsMergeConflict { get; set; }
        [ClassifyIgnoreIfDefault]
        public string ApprovedBy { get; set; }
        [ClassifyIgnoreIfDefault]
        public string DeletedBy { get; set; }

        public HistoryEntry(DateTime time, T entry, bool isSuggestion, string approvedBy)
        {
            Time = time;
            Entry = entry;
            IsSuggestion = isSuggestion;
            ApprovedBy = approvedBy;
            IsMergeConflict = false;
        }
        private HistoryEntry() { }  // for Classify

        public void TryMerge(T baseEntry, T theirsEntry)
        {
            if (baseEntry == null)
                throw new ArgumentNullException(nameof(baseEntry));
            if (theirsEntry == null)
                throw new ArgumentNullException(nameof(theirsEntry));
            tryMerge(baseEntry, Entry, theirsEntry);
        }

        private void tryMerge<TElem>(TElem baseEntry, TElem mine, TElem theirs)
        {
            Type[] prms;

            foreach (var field in typeof(TElem).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(string) || field.FieldType.IsValueType)
                {
                    if (Equals(field.GetValue(mine), field.GetValue(baseEntry)))
                        field.SetValue(mine, field.GetValue(theirs));
                    else if (Equals(field.GetValue(mine), field.GetValue(theirs)))
                    {
                        // Both sides made the same change
                    }
                    else if (Equals(field.GetValue(baseEntry), field.GetValue(theirs)))
                    {
                        // Change in mine that doesn’t conflict
                    }
                    else
                        IsMergeConflict = true;
                }

                else if (field.FieldType.TryGetGenericParameters(typeof(ICollection<>), out prms) && prms[0] == typeof(string))
                {
                    var mineList = ((ICollection<string>) field.GetValue(mine));
                    var inBase = ((ICollection<string>) field.GetValue(baseEntry)).ToHashSet();
                    var inMine = mineList.ToHashSet();
                    var inTheirs = ((ICollection<string>) field.GetValue(theirs)).ToHashSet();

                    foreach (var added in inTheirs.Except(inBase))
                        inMine.Add(added);
                    foreach (var removed in inBase.Except(inTheirs))
                        inMine.Remove(removed);

                    if (mineList is string[])
                        mineList = inMine.ToArray();
                    else
                    {
                        mineList.Clear();
                        foreach (var val in inMine)
                            mineList.Add(val);
                    }
                }

                else if (field.FieldType == typeof(ListSorted<KtaneModuleInfo>))
                {
                    var inBase = ((IEnumerable<KtaneModuleInfo>) field.GetValue(baseEntry)).ToDictionary(m => m.Name);
                    var mineList = ((ListSorted<KtaneModuleInfo>) field.GetValue(mine));
                    var inMine = mineList.ToDictionary(m => m.Name);
                    var inTheirs = ((IEnumerable<KtaneModuleInfo>) field.GetValue(theirs)).ToDictionary(m => m.Name);

                    foreach (var added in inTheirs.Keys.Except(inBase.Keys))
                    {
                        if (inMine.ContainsKey(added))
                            IsMergeConflict = true;
                        else
                            inMine[added] = inTheirs[added];
                    }
                    foreach (var removed in inBase.Keys.Except(inTheirs.Keys))
                    {
                        if (inMine.ContainsKey(removed))
                            inMine.Remove(removed);
                        else
                            IsMergeConflict = true;
                    }
                    foreach (var common in inMine.Keys.Intersect(inTheirs.Keys).Intersect(inBase.Keys))
                        tryMerge(inBase[common], inMine[common], inTheirs[common]);

                    mineList.Clear();
                    mineList.AddRange(inMine.Values);
                }

                else
                    IsMergeConflict = true;
            }
        }
    }
}
