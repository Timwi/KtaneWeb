using System;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse jsonPage(HttpRequest req, KtaneWebSession session)
        {
            if (req.Url.Path == "/raw")
                return HttpResponse.Json(ClassifyJson.Serialize(_config.Current), HttpStatusCode._200_OK, new HttpResponseHeaders { AccessControlAllowOrigin = "*" });

            var editable = session.Username != null && _config.Current.AllowedEditors.Contains(session.Username);
            Match match;

            if (req.Method == HttpMethod.Get && req.Url.Path == "")
                return jsonDefaultPage(req, editable);

            else if (req.Method == HttpMethod.Post && req.Url.Path == "/submit" && req.Post["json"].Value != null)
                return jsonSubmitEdit(req, editable, session.Username);

            else if (req.Method == HttpMethod.Post && editable && req.Url.Path == "/suggestion" && req.Post["time"].Value != null)
                return jsonAccRejSuggestion(req, editable, session.Username);

            else if (req.Method == HttpMethod.Post && editable && req.Url.Path == "/delete" && req.Post["time"].Value != null)
                return jsonDelete(req, session.Username);

            else if (req.Method == HttpMethod.Get &&
                (match = Regex.Match(req.Url.Path, @"^/diff/([^/]+)")).Success &&
                ExactConvert.Try(match.Groups[1].Value.UrlUnescape(), out DateTime dt))
            {
                var ix = _config.History.IndexOf(h => h.Time == dt);
                if (ix != -1)
                    return jsonDiff(req, _config.History[ix]);
                ix = _config.HistoryDeleted.IndexOf(h => h.Time == dt);
                if (ix != -1)
                    return jsonDiff(req, _config.HistoryDeleted[ix]);
            }

            return HttpResponse.Redirect(req.Url.WithPath("").ToHref());
        }

        private HttpResponse jsonDelete(HttpRequest req, string username)
        {
            try
            {
                var time = ExactConvert.ToDateTime(req.Post["time"].Value);
                var ix = _config.History.IndexOf(e => e.Time == time);
                if (ix != -1)
                {
                    lock (_config)
                    {
                        _config.History[ix].DeletedBy = username;
                        _config.HistoryDeleted.Add(_config.History[ix]);
                        _config.History.RemoveAt(ix);
                        saveConfig();
                    }
                }
            }
            catch (Exception e)
            {
                return jsonDefaultPage(req, editable: true, error: $"{e.Message} ({e.GetType().Name})");
            }
            return HttpResponse.Redirect(req.Url.WithPath("").ToHref());
        }

        private HttpResponse jsonDiff(HttpRequest req, HistoryEntry<KtaneWebConfigEntry> entry)
        {
            const int context = 8;
            var newTxt = ClassifyJson.Serialize(entry.Entry).ToStringIndented().Replace("\r", "");
            var oldEntry = getPrevNonSuggestion(entry);
            var oldTxt = oldEntry == null ? "" : ClassifyJson.Serialize(oldEntry).ToStringIndented().Replace("\r", "");
            var chunks = Ut.Diff(oldTxt.Split('\n'), newTxt.Split('\n')).GroupConsecutiveBy(tup => tup.Item2 == DiffOp.None).ToArray();

            return jsonPageResponse(req, new PRE { class_ = "diff" }._(chunks.Select((chunk, i) =>
            {
                object processLine(Tuple<string, DiffOp> line) => new DIV { class_ = line.Item2 == DiffOp.None ? null : line.Item2 == DiffOp.Ins ? "ins" : "del" }._(line.Item1);
                if (chunk.Key && i == 0 && chunk.Count > context + 1)
                    return new object[] { new SPAN { class_ = "sep" }, chunk.TakeLast(context).Select(processLine) };
                else if (chunk.Key && i == chunks.Length - 1 && chunk.Count > context + 1)
                    return new object[] { chunk.Take(context).Select(processLine), new SPAN { class_ = "sep" } };
                else if (chunk.Key && chunk.Count > 2 * context + 1)
                    return new object[] { chunk.Take(context).Select(processLine), new SPAN { class_ = "sep" }, chunk.TakeLast(context).Select(processLine) };
                else
                    return chunk.Select(processLine);
            })));
        }

        private HttpResponse jsonAccRejSuggestion(HttpRequest req, bool editable, string username)
        {
            try
            {
                var time = ExactConvert.ToDateTime(req.Post["time"].Value);
                var entry = _config.History.FirstOrDefault(e => e.Time == time);
                var accept = req.Post["accept"].Value == "1";
                if (entry != null && (accept || req.Post["accept"].Value == "0"))
                {
                    lock (_config)
                    {
                        if (accept)
                        {
                            var baseEntry = _config.Current;
                            entry.IsSuggestion = false;
                            entry.ApprovedBy = username;

                            // Try to merge the accepted suggestion into every other pending suggestion
                            for (int i = 0; i < _config.History.Count; i++)
                            {
                                var entry2 = _config.History[i];
                                if (!entry2.IsSuggestion)
                                    continue;
                                _config.History[i].Entry = ClassifyJson.Deserialize<KtaneWebConfigEntry>(ClassifyJson.Serialize(entry2.Entry));
                                _config.History[i].TryMerge(baseEntry, entry.Entry);
                            }
                        }
                        else
                        {
                            entry.DeletedBy = username;
                            _config.History.Remove(entry);
                            _config.HistoryDeleted.Add(entry);
                        }
                        saveConfig();
                    }
                }
            }
            catch (Exception e)
            {
                return jsonDefaultPage(req, editable, error: $"{e.Message} ({e.GetType().Name})");
            }
            return HttpResponse.Redirect(req.Url.WithPath("").ToHref());
        }

        private HttpResponse jsonSubmitEdit(HttpRequest req, bool editable, string username)
        {
            var content = req.Post["json"].Value;
            if (_config.History.Count(h => h.IsSuggestion) >= 5)
                return jsonDefaultPage(req, editable, content, error: $"Too many pending suggestions. Give me a break.");
            try
            {
                var newEntry = ClassifyJson.Deserialize<KtaneWebConfigEntry>(JsonValue.Parse(content));
                if (_config.History.Count > 0 && newEntry.Equals(_config.History[0].Entry))
                    return jsonDefaultPage(req, editable, content, error: $"Ignoring edit because no changes were made compared to the newest revision.");
                if (newEntry.KtaneModules.ConsecutivePairs(false).Any(pair => pair.Item1.Name == pair.Item2.Name))
                    return jsonDefaultPage(req, editable, content, error: $"You can’t have two modules with the same name.");
                lock (_config)
                {
                    _config.History.Add(new HistoryEntry<KtaneWebConfigEntry>(DateTime.UtcNow, newEntry, !editable, editable ? username : null));
                    saveConfig();
                }
            }
            catch (Exception e)
            {
                return jsonDefaultPage(req, editable, content, error: $"{e.Message} ({e.GetType().Name})");
            }
            return HttpResponse.Redirect(req.Url.WithPath("").ToHref());
        }

        private HttpResponse jsonDefaultPage(HttpRequest req, bool editable, string content = null, string error = null)
        {
            var anySuggestions = _config.History.Any(h => h.IsSuggestion);
            return jsonPageResponse(req, Ut.NewArray<object>(
                error.NullOr(err => new DIV { class_ = "error" }._(err)),
                new FORM { method = method.post, action = req.Url.WithPath("/submit").ToHref() }._(
                    new TEXTAREA { name = "json", accesskey = "," }._(content ?? ClassifyJson.Serialize(_config.Current).ToStringIndented()),
                    new DIV(new BUTTON { type = btype.submit, accesskey = "s" }._(editable ? "Save".Accel('S') : "Suggest".Accel('S')))),
                new H2("History"),
                new TABLE { class_ = "json-history" }._(
                    new TR(anySuggestions || editable ? new TH() : null, new TH("User"), new TH("Date/time"), new TH("Modules changed")),
                    _config.History.Concat(_config.HistoryDeleted).OrderByDescending(h => h.Time).Select(entry => new TR { class_ = entry.DeletedBy == null ? null : "deleted" }._(
                        !anySuggestions && !editable ? null : new TD { class_ = "commands" }._(
                            entry.IsSuggestion
                                ? editable && entry.DeletedBy == null
                                    ? (object) new FORM { method = method.post, action = req.Url.WithPath("/suggestion").ToHref() }._(
                                        new INPUT { type = itype.hidden, name = "time", value = ExactConvert.ToString(entry.Time) },
                                        new BUTTON { type = btype.submit, name = "accept", value = "1" }._("Accept"), " ",
                                        new BUTTON { type = btype.submit, name = "accept", value = "0" }._("Reject"))
                                    : "(suggestion)"
                                : editable && entry.DeletedBy == null
                                    ? new FORM { method = method.post, action = req.Url.WithPath("/delete").ToHref() }._(
                                        new INPUT { type = itype.hidden, name = "time", value = ExactConvert.ToString(entry.Time) },
                                        new BUTTON { type = btype.submit }._("Delete"))
                                    : null),
                        new TD { class_ = "user" }._(entry.ApprovedBy ?? "(unknown)", entry.DeletedBy == null ? null : $" (deleted by {entry.DeletedBy})"),
                        new TD { class_ = "time" }._(new A { href = req.Url.WithPath("/diff/" + ExactConvert.ToString(entry.Time)).ToHref() }._(entry.Time.ToIsoString(IsoDatePrecision.Minutes, includeTimezone: false))),
                        new TD { class_ = "changes" }._(new Func<object>(() =>
                        {
                            var prev = getPrevNonSuggestion(entry);
                            if (prev == null)
                                return "(first entry)";

                            var prevModules = prev.KtaneModules.Except(entry.Entry.KtaneModules).ToArray();
                            var newModules = entry.Entry.KtaneModules.Except(prev.KtaneModules).ToArray();

                            return prevModules.Union(newModules)
                                .Select(t => t.Name).Distinct().Order()
                                .Select(n =>
                                {
                                    var prevModule = prevModules.FirstOrDefault(pm => pm.Name == n);
                                    var newModule = newModules.FirstOrDefault(nm => nm.Name == n);
                                    return new SPAN { class_ = "module", title = prevModule == null ? "added" : newModule == null ? "deleted" : prevModule.Differences(newModule).JoinString(", ") }._(n);
                                }).DefaultIfEmpty<object>("(none)").InsertBetween(", ");
                        })))))));
        }

        private KtaneWebConfigEntry getPrevNonSuggestion(HistoryEntry<KtaneWebConfigEntry> entry)
        {
            return _config.History.FirstOrDefault(e => e.Time < entry.Time)?.Entry;
        }

        private static HttpResponse jsonPageResponse(HttpRequest req, object body)
        {
            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages — raw JSON data"),
                    new LINK { href = req.Url.WithParent("HTML/css/font.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("HTML/css/dark-theme.css").ToHref(), id = "theme-css", rel = "stylesheet", type = "text/css" },
                    new SCRIPTLiteral("if(localStorage.getItem('theme')!=='dark')document.getElementById('theme-css').setAttribute('href','')"),
                    new META { name = "viewport", content = "width=device-width" }),
                new BODY(
                    new DIV { class_ = "links" }._(new A { href = req.Url.WithPathParent().WithPathOnly("/").ToHref(), accesskey = "b" }._("Back".Accel('B'))),
                    new H1("JSON"),
                    body)));
        }
    }
}
