using System;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Generexes;
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
            var editable = session.Username != null && _config.Current.AllowedEditors.Contains(session.Username);
            int ix;
            DateTime dt;
            Match match;

            if (req.Method == HttpMethod.Get && req.Url.Path == "")
                return jsonDefaultPage(req, editable);

            else if (req.Method == HttpMethod.Post && req.Url.Path == "/submit" && req.Post["json"].Value != null)
                return jsonSubmitEdit(req, editable);

            else if (req.Method == HttpMethod.Post && editable && req.Url.Path == "/suggestion" && req.Post["time"].Value != null)
                return jsonAccRejSuggestion(req, editable);

            else if (req.Method == HttpMethod.Post && editable && req.Url.Path == "/delete" && req.Post["time"].Value != null)
                return jsonDelete(req);

            else if (req.Method == HttpMethod.Get &&
                (match = Regex.Match(req.Url.Path, @"^/diff/([^/]+)")).Success &&
                ExactConvert.Try(match.Groups[1].Value.UrlUnescape(), out dt) &&
                (ix = _config.History.IndexOf(h => h.Time == dt)) != -1)
                return jsonDiff(req, ix);

            return HttpResponse.Redirect(req.Url.WithPath("").ToHref());
        }

        private HttpResponse jsonDelete(HttpRequest req)
        {
            try
            {
                var time = ExactConvert.ToDateTime(req.Post["time"].Value);
                var ix = _config.History.IndexOf(e => e.Time == time);
                if (ix != -1)
                {
                    lock (_config)
                    {
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

        private HttpResponse jsonDiff(HttpRequest req, int ix)
        {
            const int context = 8;
            var newTxt = ClassifyJson.Serialize(_config.History[ix].Entry).ToStringIndented();
            var oldEntry = getPrevNonSuggestion(ix);
            var oldTxt = oldEntry == null ? "" : ClassifyJson.Serialize(oldEntry).ToStringIndented();
            var tokenize = Ut.Lambda((string str) => Regex.Matches(str.Replace("\r", ""), @"\w+|.", RegexOptions.Singleline).Cast<Match>().Select(m => m.Value));
            var diff = Ut.Diff(tokenize(oldTxt), tokenize(newTxt)).Select(tup => Tuple.Create(tup.Item1, (DiffOp?) tup.Item2)).ToArray();

            var unchNL = diff.CreateGenerex(tup => tup.Item2 == DiffOp.None && tup.Item1 == "\n");
            var unchNonNL = diff.CreateGenerex(tup => tup.Item2 == DiffOp.None && tup.Item1 != "\n");
            var line =
                unchNL.Process(m => false).Or(diff.CreateStartGenerex().Process(m => true)).LookBehind()
                    .Then(unchNonNL.RepeatGreedy(), (s, m) => new { Start = s, Match = m.Match })
                    .ThenRaw(unchNL.Process(m => false).Or(diff.CreateEndGenerex().Process(m => true)),
                        (prev, f) => new { prev.Start, Match = prev.Match.Concat(new Tuple<string, DiffOp?>("\n", DiffOp.None)).ToArray(), End = f });

            var diff2 = line.RepeatGreedy(min: context + 1).ProcessRaw(m => m.ToArray()).ReplaceRaw(diff,
                m => m.Length > 2 * context || m[0].Start || m.Last().End
                    ? m.Subarray(0, m[0].Start ? 0 : context).SelectMany(x => x.Match).Concat(new Tuple<string, DiffOp?>(null, null)).Concat(m.Subarray(m.Length - (m.Last().End ? 0 : context)).SelectMany(x => x.Match))
                    : m.SelectMany(x => x.Match));

            return jsonPage(req, new PRE { class_ = "diff" }._(diff2.GroupConsecutiveBy(tup => tup.Item2).Select(gr =>
                gr.Key == null ? new SPAN { class_ = "sep" } :
                new SPAN { class_ = gr.Key == DiffOp.None ? null : gr.Key == DiffOp.Ins ? "ins" : "del" }._(gr.Select(tup => tup.Item1 == "\n" && tup.Item2 != DiffOp.None ? "⏎\n" : tup.Item1)))));
        }

        private HttpResponse jsonAccRejSuggestion(HttpRequest req, bool editable)
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
                            _config.History.Remove(entry);
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

        private HttpResponse jsonSubmitEdit(HttpRequest req, bool editable)
        {
            var content = req.Post["json"].Value;
            if (_config.History.Count(h => h.IsSuggestion) >= 3)
                return jsonDefaultPage(req, editable, content, error: $"Too many pending suggestions. Give me a break.");
            try
            {
                var newEntry = ClassifyJson.Deserialize<KtaneWebConfigEntry>(JsonValue.Parse(content));
                if (_config.History.Count > 0 && newEntry.Equals(_config.History[0].Entry))
                    return jsonDefaultPage(req, editable, content, error: $"Ignoring edit because no changes were made compared to the newest revision.");
                lock (_config)
                {
                    _config.History.Add(new HistoryEntry<KtaneWebConfigEntry>(DateTime.UtcNow, newEntry, !editable));
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
            return jsonPage(req, Ut.NewArray<object>(
                error.NullOr(err => new DIV { class_ = "error" }._(err)),
                new FORM { method = method.post, action = req.Url.WithPath("/submit").ToHref() }._(
                    new TEXTAREA { name = "json", accesskey = "," }._(content ?? ClassifyJson.Serialize(_config.Current).ToStringIndented()),
                    new DIV(new BUTTON { type = btype.submit, accesskey = "s" }._(editable ? "Save".Accel('S') : "Suggest".Accel('S')))),
                new H2("History"),
                new TABLE { class_ = "json-history" }._(
                    new TR(anySuggestions || editable ? new TH() : null, new TH("Date/time"), new TH("Modules changed")),
                    _config.History.Select((entry, i) => req.Url.WithPath("/diff/" + ExactConvert.ToString(entry.Time)).ToHref().Apply(url => new TR(
                        !anySuggestions && !editable ? null : new TD { class_ = "commands" }._(
                            entry.IsSuggestion
                                ? editable
                                    ? (object) new FORM { method = method.post, action = req.Url.WithPath("/suggestion").ToHref() }._(
                                        new INPUT { type = itype.hidden, name = "time", value = ExactConvert.ToString(entry.Time) },
                                        new BUTTON { type = btype.submit, name = "accept", value = "1" }._("Accept"), " ",
                                        new BUTTON { type = btype.submit, name = "accept", value = "0" }._("Reject"))
                                    : "(suggestion)"
                                : editable
                                    ? (object) new FORM { method = method.post, action = req.Url.WithPath("/delete").ToHref() }._(
                                        new INPUT { type = itype.hidden, name = "time", value = ExactConvert.ToString(entry.Time) },
                                        new BUTTON { type = btype.submit }._("Delete"))
                                    : null),
                        new TD { class_ = "time" }._(new A { href = url }._(entry.Time.ToIsoString(IsoDatePrecision.Minutes, includeTimezone: false))),
                        new TD { class_ = "changes" }._(new A { href = url }._(getPrevNonSuggestion(i).Apply(prevEntry => prevEntry == null
                            ? (object) "(first entry)"
                            : prevEntry.KtaneModules.Except(entry.Entry.KtaneModules).Union(entry.Entry.KtaneModules.Except(prevEntry.KtaneModules))
                                .Select(t => t.Name).Distinct().Order().Select(n => new SPAN { class_ = "module" }._(n)).DefaultIfEmpty<object>("(none)").InsertBetween(", "))))))))));
        }

        private KtaneWebConfigEntry getPrevNonSuggestion(int index)
        {
            var oldIx = index + 1;
            while (oldIx < _config.History.Count && _config.History[oldIx].IsSuggestion)
                oldIx++;
            return oldIx == _config.History.Count ? null : _config.History[oldIx].Entry;
        }

        private static HttpResponse jsonPage(HttpRequest req, object body)
        {
            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages — raw JSON data"),
                    new LINK { href = req.Url.WithParent("HTML/css/font.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new META { name = "viewport", content = "width=device-width" }),
                new BODY(
                    new DIV { class_ = "links" }._(new A { href = req.Url.WithPathParent().WithPathOnly("/").ToHref(), accesskey = "b" }._("Back".Accel('B'))),
                    new H1("JSON"),
                    body)));
        }
    }
}
