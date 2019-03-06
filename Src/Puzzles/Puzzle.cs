using RT.Util.Serialization;

namespace KtaneWeb.Puzzles
{
    sealed class Puzzle
    {
        public string Title = "Untitled puzzle";
        public string Filename = "Untitled puzzle.html";
        public string Author = null;
        public bool IsPublished = false;
        public bool IsNew = false;

        [ClassifyIgnoreIfDefault]
        public bool MovingMark = false;
    }
}
