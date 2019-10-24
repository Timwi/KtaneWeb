using System.Collections.Generic;
using RT.Serialization;

namespace KtaneWeb.Puzzles
{
    sealed class PuzzleInfo
    {
        [ClassifyIgnoreIfDefault]
        public string JavaScriptFile = null;
        [ClassifyIgnoreIfDefault]
        public string CssFile = null;

        [ClassifyNotNull]
        public List<PuzzleGroup> PuzzleGroups = new List<PuzzleGroup>();

        [ClassifyNotNull]
        public List<string> EditAccess = new List<string> { "Timwi" };
    }
}
