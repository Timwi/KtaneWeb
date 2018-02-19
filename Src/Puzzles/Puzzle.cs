using RT.Util.Serialization;

namespace KtaneWeb.Puzzles
{
    sealed class Puzzle
    {
        public string Title = "Untitled puzzle";
        public string Filename = "Untitled puzzle.html";
        public bool IsPublished = false;

        [ClassifyIgnoreIfDefault]
        public bool MovingMark = false;
    }
}
