using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb.Puzzles
{
    sealed class Api
    {
        private readonly PuzzleInfo _puzzles;
        private readonly string _puzzleDir;
        private readonly KtaneWebSession _session;
        private readonly Action _save;

        private bool canEdit() => _puzzles.EditAccess.Contains(_session.Username);
        private bool canView(PuzzleGroup gr) => gr.ViewAccess.Contains(_session.Username) || canEdit(gr);
        private bool canEdit(PuzzleGroup gr) => gr.EditAccess.Contains(_session.Username);

        public Api(KtaneWebConfig config, KtaneWebSession session, Action save = null)
        {
            _puzzles = (config ?? throw new ArgumentNullException(nameof(config))).Puzzles;
            _puzzleDir = Path.Combine(config.BaseDir, "puzzles");
            _session = session;
            _save = save;
        }

        public IEnumerable<object> RenderBody(string error = null)
        {
            if (error != null)
                yield return new DIV { class_ = "error" }._(error);

            yield return new DIV { class_ = "header" }._(
                new STRONG("Welcome to the KTANE Puzzle page!"), " ",
                @"On this page you will find collections of puzzles that are themed around the game of ",
                new EM("Keep Talking and Nobody Explodes"), " and its modded content. Most of the puzzles are heavily and pervasively themed in this manner. " +
                "The target audience is players who are familiar with a great majority of modules. If you have not played modded KTANE much, you can still solve " +
                "the puzzles since technically ", new A { href = "https://ktane.timwi.de" }._("all the required information is available"), ", but brace yourself for a lot of research.");

            if (canEdit() || _puzzles.PuzzleGroups.Any(gr => canEdit(gr)))
                yield return new MENU { class_ = "controls req-priv" }._(
                    !canEdit() ? null : new LI(new BUTTON { class_ = "operable" }.Data("fn", nameof(AddGroup))._("Add puzzle group")),
                    new LI(new BUTTON { id = "show-pristine" }._("Show pristine")));

            yield return _puzzles.PuzzleGroups.Where(gr => gr.IsPublished || canView(gr)).OrderByDescending(gr => gr.Ordering).Select(group => Ut.NewArray<object>(
                File.Exists(Path.Combine(_puzzleDir, group.Folder, "Logo.png"))
                    ? new H1 { class_ = "logo" + (group.IsPublished ? " published" : " req-priv") }._(new IMG { class_ = "logo", src = $"{group.Folder}/Logo.png", alt = group.Title })
                    : new H1 { class_ = "text" + (group.IsPublished ? " published" : " req-priv") }._(group.Title.Select(ch => new SPAN(ch))),
                new DIV { class_ = "puzzle-group" + (group.IsPublished ? " published" : " req-priv") + (canEdit(group) ? " editable" : "") }._(

                    // Group title
                    new DIV { class_ = "title" }._(
                        group.Title,
                        editIcon(nameof(RenameGroup), group, group.Title, tooltip: "Edit title")),

                    new DIV { class_ = "group-info" }._(
                        // Author
                        new DIV { class_ = "author" }._(group.Author, editIcon(nameof(ChangeGroupAuthor), group, group.Author, tooltip: "Edit author")),

                        // Ordering (only shown with editing privs)
                        !canEdit(group) ? null : new DIV { class_ = "ordering req-priv" }._(group.Ordering, editIcon(nameof(ChangeGroupOrdering), group, group.Ordering.ToString(), tooltip: "Change ordering"))),

                    // List of puzzles
                    new DIV { class_ = "puzzles" }._(
                        group.Puzzles.Where(puzzle => puzzle.IsPublished || canView(group)).Select((puzzle, ix) => new DIV { class_ = "puzzle" + (puzzle.IsPublished ? "" : " req-priv") + (puzzle.IsNew ? " new" : null) + (File.Exists(Path.Combine(_puzzleDir, group.Folder, puzzle.Filename)) ? null : " missing") }._(

                            // “Move here” (absolutely positioned)
                            canEdit(group) && group.Puzzles.Any(p => p.MovingMark) ? new BUTTON { class_ = "operable req-priv move-here" }.Data("fn", nameof(MovePuzzle)).Data("groupname", group.Title).Data("index", ix)._("move here") : null,

                            // Right-floating stuff
                            new DIV { class_ = "puzzle-buttons" }._(
                                !canEdit(group) ? null : Ut.NewArray<object>(
                                    // Mark/unmark new
                                    new BUTTON { class_ = "operable req-priv" }.Data("fn", nameof(TogglePuzzleNew)).Data("groupname", group.Title).Data("puzzlename", puzzle.Title)._(puzzle.IsNew ? "unmark new" : "mark new"),
                                    // Move
                                    new BUTTON { class_ = "operable req-priv" + (puzzle.MovingMark ? " perm" : "") }.Data("fn", nameof(MovePuzzleMark)).Data("groupname", group.Title).Data("puzzlename", puzzle.Title)._(puzzle.MovingMark ? "move where?" : "move"),
                                    // Publish/hide
                                    new BUTTON { class_ = "operable req-priv" }.Data("fn", puzzle.IsPublished ? nameof(UnpublishPuzzle) : nameof(PublishPuzzle)).Data("groupname", group.Title).Data("puzzlename", puzzle.Title)._(puzzle.IsPublished ? "hide" : "publish")),

                                // Author and date
                                puzzle.Author == null && puzzle.Date == null ? null : new DIV { class_ = "puzzle-info" }._(
                                    puzzle.Author.NullOr(a => new DIV { class_ = "puzzle-author" }._(a)),
                                    puzzle.Date.NullOr(d => new DIV { class_ = "puzzle-date" }._(d.ToString("yyyy-MM-dd")))),

                                // Edit author
                                editIcon(nameof(EditPuzzleAuthor), group, puzzle, puzzle.Author, tooltip: "Edit author"),
                                // Edit date
                                editIcon(nameof(EditPuzzleDate), group, puzzle, puzzle.Date.NullOr(d => d.ToString("yyyy-MM-dd")) ?? "", tooltip: "Edit publication date"),

                                // Check answer
                                puzzle.Answer == null ? null : new Func<object>(() =>
                                {
                                    using var sha = SHA256.Create();
                                    return new SPAN { class_ = "check-answer-span" }._(
                                        new INPUT { type = itype.text, class_ = "check-answer" },
                                        new BUTTON { class_ = "check-answer-btn button" }.Data("sha256", sha.ComputeHash(puzzle.Answer.ToUpperInvariant().Where(ch => (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')).JoinString().ToUtf8()).ToHex())._("Check answer"),
                                        new SPAN { class_ = "correct-answer" });
                                }),
                                // View solution
                                !File.Exists(Path.Combine(_puzzleDir, group.Folder, puzzle.SolutionFilename)) ? null : new A { class_ = "button", href = $"{group.Folder}/{puzzle.SolutionFilename}" }._("View solution")),

                            // Puzzle title
                            new A { href = $"{group.Folder}/{puzzle.Filename}", class_ = "puzzle-title" }._(puzzle.Title),
                            // Edit puzzle title
                            editIcon(nameof(RenamePuzzle), group, puzzle, puzzle.Title, extraClass: "name-edit-icon", tooltip: "Edit puzzle title"),
                            // Edit puzzle answer (if no answer given)
                            puzzle.Answer != null ? null : editIcon(nameof(EditPuzzleAnswer), group, puzzle, puzzle.Answer, tooltip: "Edit puzzle answer"),

                            // Puzzle answer
                            !canEdit(group) || puzzle.Answer == null ? null : new DIV { class_ = "req-priv puzzle-answer" }._(
                                puzzle.Answer,
                                editIcon(nameof(EditPuzzleAnswer), group, puzzle, puzzle.Answer, tooltip: "Edit answer")),

                            // “Move here” at the bottom of the last element
                            ix == group.Puzzles.Count - 1 && canEdit(group) && group.Puzzles.Any(p => p.MovingMark) ? new BUTTON { class_ = "operable req-priv move-here" }.Data("fn", nameof(MovePuzzle)).Data("groupname", group.Title).Data("index", group.Puzzles.Count)._("move here") : null))),

                    canEdit(group) ? new MENU { class_ = "controls req-priv" }._(
                        new LI(new BUTTON { class_ = "operable" }.Data("fn", group.IsPublished ? nameof(UnpublishGroup) : nameof(PublishGroup)).Data("groupname", group.Title)._(group.IsPublished ? "Hide" : "Publish")),
                        new LI(new BUTTON { class_ = "operable" }.Data("fn", nameof(AddPuzzle)).Data("groupname", group.Title)._("Add puzzle")),
                        new LI(new SPAN { class_ = "folder" }._(group.Folder, editIcon(nameof(ChangeGroupFolder), group, group.Folder, tooltip: "Change folder name")))
                    ) : null
                )));
        }

        private object editIcon(string fn, PuzzleGroup group, string prevValue, string extraClass = null, string tooltip = null) =>
            canEdit(group) ? new BUTTON { class_ = "edit-icon req-priv operable" + (extraClass == null ? null : " " + extraClass), title = tooltip }.Data("fn", fn).Data("groupname", group.Title).Data("query", prevValue ?? "") : null;
        private object editIcon(string fn, PuzzleGroup group, Puzzle puzzle, string prevValue, string extraClass = null, string tooltip = null) =>
            canEdit(group) ? new BUTTON { class_ = "edit-icon req-priv operable" + (extraClass == null ? null : " " + extraClass), title = tooltip }.Data("fn", fn).Data("groupname", group.Title).Data("puzzlename", puzzle.Title).Data("query", prevValue ?? "") : null;

        public string RenderBodyStr(string error = null) => Tag.ToString(RenderBody(error));

        [AjaxMethod]
        public string AddGroup()
        {
            if (!canEdit())
                return RenderBodyStr("You do not have access to edit puzzles.");
            var newGroup = new PuzzleGroup();
            var already = _puzzles.PuzzleGroups.FirstOrDefault(gr => gr.Title.EqualsIgnoreCase(newGroup.Title));
            if (already != null)
                return RenderBodyStr($"There is already a puzzle group titled “{already.Title}”. Please rename that group first.");

            if (!newGroup.ViewAccess.Contains(_session.Username))
                newGroup.ViewAccess.Add(_session.Username);
            if (!newGroup.EditAccess.Contains(_session.Username))
                newGroup.EditAccess.Add(_session.Username);
            _puzzles.PuzzleGroups.Add(newGroup);
            _save();
            return RenderBodyStr();
        }

        private string editGroup(string groupname, Action<PuzzleGroup> action)
        {
            var group = _puzzles.PuzzleGroups.FirstOrDefault(gr => gr.Title.EqualsIgnoreCase(groupname));
            if (group != null)
            {
                if (!canEdit(group))
                    return RenderBodyStr("You do not have access to edit this puzzle group.");
                try { action(group); }
                catch (Exception e) { return RenderBodyStr(e.Message); }
                _save();
            }
            return RenderBodyStr();
        }

        [AjaxMethod]
        public string RenameGroup(string groupname, string query)
        {
            var already = groupname.EqualsIgnoreCase(query) ? null : _puzzles.PuzzleGroups.FirstOrDefault(gr => gr.Title.EqualsIgnoreCase(query));
            return already != null
                ? RenderBodyStr($"There is already a puzzle group titled “{already.Title}”.")
                : editGroup(groupname, gr => { gr.Title = query; });
        }

        [AjaxMethod]
        public string PublishGroup(string groupname) => editGroup(groupname, gr => { gr.IsPublished = true; });
        [AjaxMethod]
        public string UnpublishGroup(string groupname) => editGroup(groupname, gr => { gr.IsPublished = false; });
        [AjaxMethod]
        public string ChangeGroupAuthor(string groupname, string query) => editGroup(groupname, gr => { gr.Author = query; });
        [AjaxMethod]
        public string ChangeGroupFolder(string groupname, string query) => editGroup(groupname, gr => { gr.Folder = query; });
        [AjaxMethod]
        public string ChangeGroupOrdering(string groupname, string query)
        {
            var allGroups = _puzzles.PuzzleGroups.ToArray();
            if (!double.TryParse(query, out var newValue))
                return RenderBodyStr("That is not a valid number.");
            if (allGroups.FirstOrDefault(gr => gr.Title.EqualsIgnoreCase(groupname)) is { } group)
            {
                if (!canEdit(group))
                    return RenderBodyStr("You do not have access to edit this puzzle group.");
                try
                {
                    var orderedGroups = allGroups.OrderBy(gr => gr == group ? newValue : gr.Ordering).ToArray();
                    for (var i = 0; i < orderedGroups.Length; i++)
                        orderedGroups[i].Ordering = i + 1;
                }
                catch (Exception e) { return RenderBodyStr(e.Message); }
                _save();
            }
            return RenderBodyStr();
        }

        [AjaxMethod]
        public string AddPuzzle(string groupname) => editGroup(groupname, gr =>
        {
            var newPuzzle = new Puzzle();
            var already = gr.Puzzles.FirstOrDefault(pz => pz.Title.EqualsIgnoreCase(newPuzzle.Title));
            if (already != null)
                throw new Exception($"There is already a puzzle titled “{already.Title}”.");
            gr.Puzzles.Add(newPuzzle);
        });

        private string editPuzzle(string groupname, string puzzlename, Action<PuzzleGroup, Puzzle> action) => editGroup(groupname, group =>
        {
            var puzzle = group.Puzzles.FirstOrDefault(pz => pz.Title.EqualsIgnoreCase(puzzlename));
            if (puzzle != null)
            {
                action(group, puzzle);
                _save();
            }
        });

        [AjaxMethod]
        public string RenamePuzzle(string groupname, string puzzlename, string query) => editPuzzle(groupname, puzzlename, (group, puzzle) =>
        {
            var already = puzzlename.EqualsIgnoreCase(query) ? null : group.Puzzles.FirstOrDefault(pz => pz.Title.EqualsIgnoreCase(query));
            if (already != null)
                throw new Exception($"There is already a puzzle titled “{already.Title}”.");
            var newFilenameStub = Regex.Replace(query, @"[\*\?<>/#\\&%]", "_").Trim();
            var newFilename = newFilenameStub + ".html";
            already = group.Puzzles.FirstOrDefault(pz => pz != puzzle && pz.Filename.EqualsIgnoreCase(newFilename));
            if (already != null)
                throw new Exception($"There is already a puzzle with the filename “{already.Filename}”.");
            puzzle.Title = query;
            puzzle.Filename = newFilename;
            puzzle.SolutionFilename = newFilenameStub + " solution.html";
        });

        [AjaxMethod]
        public string TogglePuzzleNew(string groupname, string puzzlename) => editPuzzle(groupname, puzzlename, (group, puzzle) => { puzzle.IsNew = !puzzle.IsNew; });
        [AjaxMethod]
        public string EditPuzzleAuthor(string groupname, string puzzlename, string query) => editPuzzle(groupname, puzzlename, (group, puzzle) => { puzzle.Author = query == "" ? null : query; });
        [AjaxMethod]
        public string EditPuzzleAnswer(string groupname, string puzzlename, string query) => editPuzzle(groupname, puzzlename, (group, puzzle) => { puzzle.Answer = query == "" ? null : query; });
        [AjaxMethod]
        public string EditPuzzleDate(string groupname, string puzzlename, string query) => editPuzzle(groupname, puzzlename, (group, puzzle) =>
        {
            if (query != null && DateTime.TryParse(query, out var dt))
                puzzle.Date = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
            else
                puzzle.Date = null;
        });
        [AjaxMethod]
        public string PublishPuzzle(string groupname, string puzzlename) => editPuzzle(groupname, puzzlename, (gr, pz) => { pz.IsPublished = true; });
        [AjaxMethod]
        public string UnpublishPuzzle(string groupname, string puzzlename) => editPuzzle(groupname, puzzlename, (gr, pz) => { pz.IsPublished = false; });
        [AjaxMethod]
        public string MovePuzzleMark(string groupname, string puzzlename) => editPuzzle(groupname, puzzlename, (gr, puzzle) =>
        {
            if (puzzle.MovingMark)
                puzzle.MovingMark = false;
            else
            {
                foreach (var pz in gr.Puzzles)
                    pz.MovingMark = false;
                puzzle.MovingMark = true;
            }
        });
        [AjaxMethod]
        public string MovePuzzle(string groupname, int index) => editGroup(groupname, gr =>
        {
            var puzzleIx = gr.Puzzles.IndexOf(pz => pz.MovingMark);
            if (puzzleIx == -1)
                return;
            var puzzle = gr.Puzzles[puzzleIx];
            gr.Puzzles.RemoveAt(puzzleIx);
            gr.Puzzles.Insert(index > puzzleIx ? index - 1 : index, puzzle);
            puzzle.MovingMark = false;
        });
    }
}
