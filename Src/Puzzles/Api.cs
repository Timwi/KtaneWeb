using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb.Puzzles
{
    sealed class Api
    {
        private PuzzleInfo _puzzles;
        private string _puzzleDir;
        private KtaneWebSession _session;
        private Action _save;

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

            if (canEdit())
                yield return new MENU { class_ = "controls req-priv" }._(
                    new LI(new BUTTON { class_ = "operable" }.Data("fn", "AddGroup")._("Add puzzle group")),
                    new LI(new BUTTON { id = "show-pristine" }._("Show pristine"))
                );

            yield return _puzzles.PuzzleGroups.Where(gr => gr.IsPublished || canView(gr)).OrderByDescending(gr => gr.Published).Select(group => Ut.NewArray<object>(
                File.Exists(Path.Combine(_puzzleDir, group.Folder, "Logo.png"))
                    ? new H1 { class_ = "logo" + (group.IsPublished ? " published" : " req-priv") }._(new IMG { class_ = "logo", src = $"{group.Folder}/Logo.png", alt = group.Title })
                    : new H1 { class_ = "text" + (group.IsPublished ? " published" : " req-priv") }._(group.Title.Select(ch => new SPAN(ch))),
                new DIV { class_ = "puzzle-group" + (group.IsPublished ? " published" : " req-priv") }._(
                    new DIV { class_ = "title" }._(
                        group.Title,
                        editIcon(nameof(RenameGroup), group, group.Title)
                    ),
                    new DIV { class_ = "author" }._(group.Author, editIcon(nameof(ChangeGroupAuthor), group, group.Author)),
                    new DIV { class_ = "date-published" }._(group.Published.ToString("MMM yyyy"), editIcon(nameof(ChangeGroupMonth), group, group.Published.Month.ToString()), editIcon(nameof(ChangeGroupYear), group, group.Published.Year.ToString())),
                    new DIV { class_ = "puzzles" }._(
                        group.Puzzles.Where(puzzle => puzzle.IsPublished || canView(group)).Select((puzzle, ix) => new DIV { class_ = "puzzle" + (puzzle.IsPublished ? " published" : " req-priv") + (File.Exists(Path.Combine(_puzzleDir, group.Folder, puzzle.Filename)) ? "" : " missing") }._(
                            canEdit(group) && group.Puzzles.Any(p => p.MovingMark) ? new BUTTON { class_ = "operable req-priv move-here" }.Data("fn", nameof(MovePuzzle)).Data("groupname", group.Title).Data("index", ix)._("move here") : null,
                            new A { href = $"{group.Folder}/{puzzle.Filename}", class_ = "puzzle-inner" }._(
                                new SPAN { class_ = "puzzle-title" }._(puzzle.Title),
                                editIcon(nameof(RenamePuzzle), group, puzzle, puzzle.Title),
                                canEdit(group) ? new BUTTON { class_ = "operable req-priv" }.Data("fn", puzzle.IsPublished ? nameof(UnpublishPuzzle) : nameof(PublishPuzzle)).Data("groupname", group.Title).Data("puzzlename", puzzle.Title)._(puzzle.IsPublished ? "hide" : "publish") : null,
                                canEdit(group) ? new BUTTON { class_ = "operable req-priv" + (puzzle.MovingMark ? " perm" : "") }.Data("fn", nameof(MovePuzzleMark)).Data("groupname", group.Title).Data("puzzlename", puzzle.Title)._(puzzle.MovingMark ? "move where?" : "move") : null
                            ),
                            ix == group.Puzzles.Count - 1 && canEdit(group) && group.Puzzles.Any(p => p.MovingMark) ? new BUTTON { class_ = "operable req-priv move-here" }.Data("fn", nameof(MovePuzzle)).Data("groupname", group.Title).Data("index", group.Puzzles.Count)._("move here") : null
                        ))
                    ),
                    canEdit() ? new MENU { class_ = "controls req-priv" }._(
                        new LI(new BUTTON { class_ = "operable" }.Data("fn", group.IsPublished ? nameof(UnpublishGroup) : nameof(PublishGroup)).Data("groupname", group.Title)._(group.IsPublished ? "Hide" : "Publish")),
                        new LI(new BUTTON { class_ = "operable" }.Data("fn", nameof(AddPuzzle)).Data("groupname", group.Title)._("Add puzzle")),
                        new LI(new SPAN { class_ = "folder" }._(group.Folder, editIcon(nameof(ChangeGroupFolder), group, group.Folder)))
                    ) : null
                )));
        }

        private object editIcon(string fn, PuzzleGroup group, string prevValue) => canEdit(group) ? new BUTTON { class_ = "edit-icon req-priv operable" }.Data("fn", fn).Data("groupname", group.Title).Data("query", prevValue) : null;
        private object editIcon(string fn, PuzzleGroup group, Puzzle puzzle, string prevValue) => canEdit(group) ? new BUTTON { class_ = "edit-icon req-priv operable" }.Data("fn", fn).Data("groupname", group.Title).Data("puzzlename", puzzle.Title).Data("query", prevValue) : null;

        public string RenderBodyStr(string error = null) => Tag.ToString(RenderBody(error));

        [AjaxMethod]
        public string AddGroup()
        {
            var newGroup = new PuzzleGroup();
            var already = _puzzles.PuzzleGroups.FirstOrDefault(gr => gr.Title.EqualsNoCase(newGroup.Title));
            if (already != null)
                return RenderBodyStr($"There is already a puzzle group titled “{already.Title}”. Please rename that group first.");

            _puzzles.PuzzleGroups.Add(newGroup);
            _save();
            return RenderBodyStr();
        }

        private string editGroup(string groupname, Action<PuzzleGroup> action)
        {
            var group = _puzzles.PuzzleGroups.FirstOrDefault(gr => gr.Title.EqualsNoCase(groupname));
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
            var already = groupname.EqualsNoCase(query) ? null : _puzzles.PuzzleGroups.FirstOrDefault(gr => gr.Title.EqualsNoCase(query));
            if (already != null)
                return RenderBodyStr($"There is already a puzzle group titled “{already.Title}”.");
            return editGroup(groupname, gr => { gr.Title = query; });
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
        public string ChangeGroupMonth(string groupname, string query) => editGroup(groupname, gr => { if (int.TryParse(query, out int month) && month >= 1 && month <= 12) gr.Published = new DateTime(gr.Published.Year, month, 1); });
        [AjaxMethod]
        public string ChangeGroupYear(string groupname, string query) => editGroup(groupname, gr => { if (int.TryParse(query, out int year) && year >= 2000 && year <= 3000) gr.Published = new DateTime(year, gr.Published.Month, 1); });

        [AjaxMethod]
        public string AddPuzzle(string groupname) => editGroup(groupname, gr =>
        {
            var newPuzzle = new Puzzle();
            var already = gr.Puzzles.FirstOrDefault(pz => pz.Title.EqualsNoCase(newPuzzle.Title));
            if (already != null)
                throw new Exception($"There is already a puzzle titled “{already.Title}”.");
            gr.Puzzles.Add(newPuzzle);
        });

        private string editPuzzle(string groupname, string puzzlename, Action<PuzzleGroup, Puzzle> action) => editGroup(groupname, group =>
        {
            var puzzle = group.Puzzles.FirstOrDefault(pz => pz.Title.EqualsNoCase(puzzlename));
            if (puzzle != null)
            {
                action(group, puzzle);
                _save();
            }
        });

        [AjaxMethod]
        public string RenamePuzzle(string groupname, string puzzlename, string query) => editPuzzle(groupname, puzzlename, (group, puzzle) =>
        {
            var already = puzzlename.EqualsNoCase(query) ? null : group.Puzzles.FirstOrDefault(pz => pz.Title.EqualsNoCase(query));
            if (already != null)
                throw new Exception($"There is already a puzzle titled “{already.Title}”.");
            var newFilename = Regex.Replace(query, @"[\*\?<>/]", " ").Trim() + ".html";
            already = group.Puzzles.FirstOrDefault(pz => pz != puzzle && pz.Filename.EqualsNoCase(newFilename));
            if (already != null)
                throw new Exception($"There is already a puzzle with the filename “{already.Filename}”.");
            puzzle.Title = query;
            puzzle.Filename = newFilename;
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
