using System.Collections.Generic;
using RT.Util.Serialization;

namespace KtaneWeb.Puzzles
{
    sealed class PuzzleInfo
    {
        [ClassifyIgnoreIfDefault]
        public string JavaScriptFile;
        [ClassifyIgnoreIfDefault]
        public string CssFile;

        [ClassifyNotNull]
        public List<PuzzleGroup> PuzzleGroups = new List<PuzzleGroup>();

        [ClassifyNotNull]
        public List<string> EditAccess = new List<string> { "Timwi" };
    }
}
