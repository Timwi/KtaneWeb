using RT.Util.Serialization;

namespace KtaneWeb.Puzzles
{
    sealed class Puzzle : IClassifyObjectProcessor
    {
        public string Title = "Untitled puzzle";
        public string Filename = "Untitled puzzle.html";
        public string SolutionFilename = "Untitled puzzle solution.html";
        public string Author = null;
        public bool IsPublished = false;
        public bool IsNew = false;

        [ClassifyIgnoreIfDefault]
        public bool MovingMark = false;

        void IClassifyObjectProcessor.AfterDeserialize()
        {
            if (Filename != null && SolutionFilename == "Untitled puzzle solution.html")
                SolutionFilename = Filename.Replace(".html", " solution.html");
        }

        void IClassifyObjectProcessor.BeforeSerialize() { }
    }
}
