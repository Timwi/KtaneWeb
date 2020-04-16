using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using RT.Json;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private static readonly bool _pdfEnabled = true;
        private HttpResponse pdf(HttpRequest req)
        {
            ensureModuleInfoCache();

            string lastExaminedPdfFile = "<none>";
            try
            {
                if (!_pdfEnabled)
                    return HttpResponse.PlainText("This feature is temporarily disabled because it didn’t work for most people.");

                if (req.Method != HttpMethod.Post)
                    return HttpResponse.Redirect(req.Url.WithPathParent().WithPath(""));

                var json = JsonValue.Parse(req.Post["json"].Value);
                var pdfs = new List<string>();
                var keywords = json["search"].GetString().Length == 0 ? null : json["search"].GetString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var searchOptions = json["searchOptions"].GetList().Select(j => j.GetString()).ToArray();
                var filterEnabledByProfile = json["filterEnabledByProfile"].GetBool();
                var filterVetoedByProfile = json["filterVetoedByProfile"].GetBool();
                var profileVetoList = (filterEnabledByProfile == filterVetoedByProfile) ? null : json["profileVetoList"]?.GetList().Select(j => j.GetString()).ToArray();

                // Filter
                var matchingModules = _moduleInfoCache.Modules.Where(m =>
                {
                    if (profileVetoList != null && !(profileVetoList.Contains(m.ModuleID) ? filterVetoedByProfile : filterEnabledByProfile))
                        return false;

                    foreach (var filter in _filters)
                        if (!filter.Matches(m, json["filter"].Safe[filter.PropName].GetDictSafe()))
                            return false;

                    return keywords == null ||
                        (searchOptions.Contains("names") && keywords.All(k => m.Name.ContainsNoCase(k))) ||
                        (searchOptions.Contains("authors") && keywords.All(k => m.Author.ContainsNoCase(k))) ||
                        (searchOptions.Contains("descriptions") && keywords.All(k => m.Description.ContainsNoCase(k))) ||
                        (searchOptions.Contains("workshopids") && keywords.All(k => m.SteamID.JsEscapeNull().Contains(k)));
                });

                // Sort
                switch (json["sort"].GetString())
                {
                    case "name": matchingModules = matchingModules.OrderBy(m => m.SortKey); break;
                    case "defdiff": matchingModules = matchingModules.OrderBy(m => m.DefuserDifficulty); break;
                    case "expdiff": matchingModules = matchingModules.OrderBy(m => m.ExpertDifficulty); break;
                    case "published": matchingModules = matchingModules.OrderByDescending(m => m.Published); break;
                }

                var pdfFiles = new List<string>();

                foreach (var module in matchingModules)
                {
                    var filename = $"{module.Name}.pdf";
                    var fullPath = Path.Combine(_config.BaseDir, _config.PdfDir, filename);
                    if (json["preferredManuals"].ContainsKey(module.Name))
                    {
                        var pref = json["preferredManuals"][module.Name].GetString();
                        var preferredFilename = Regex.Replace(pref, @" \((?:PDF|HTML)\)$", ".pdf");
                        var preferredFullPath = Path.Combine(_config.BaseDir, _config.PdfDir, preferredFilename);
                        if (File.Exists(preferredFullPath))
                        {
                            fullPath = preferredFullPath;
                            filename = preferredFilename;
                        }
                    }
                    if (File.Exists(fullPath))
                        pdfFiles.Add(filename);
                }

                if (pdfFiles.Count == 0)
                    return HttpResponse.PlainText("Error: no matching PDF files found.", HttpStatusCode._500_InternalServerError);

                var list = pdfFiles.JoinString("\n");
                using (var mem = new MemoryStream(list.ToUtf8()))
                {
                    var sha1 = SHA1.Create().ComputeHash(mem).ToHex();
                    var pdfPath = Path.Combine(_config.BaseDir, _config.MergedPdfsDir, $"{sha1}.pdf");
                    if (!File.Exists(pdfPath))
                        lock (this)
                            if (!File.Exists(pdfPath))
                            {
                                var mergedPdf = new PdfDocument();
                                foreach (var pdfFile in pdfFiles)
                                {
                                    lastExaminedPdfFile = pdfFile;
                                    var pdf = PdfReader.Open(Path.Combine(_config.BaseDir, _config.PdfDir, pdfFile), PdfDocumentOpenMode.Import);
                                    int count = pdf.PageCount;
                                    for (int idx = 0; idx < count; idx++)
                                        mergedPdf.AddPage(pdf.Pages[idx]);
                                }
                                using (var f = File.OpenWrite(pdfPath))
                                    mergedPdf.Save(f);
                            }
                    return HttpResponse.Redirect(req.Url.WithPathParent().WithPathOnly($"/MergedPdfs/{sha1}.pdf"));
                }
            }
            catch (Exception e)
            {
                var exc = e;
                var sb = new StringBuilder();
                while (exc != null)
                {
                    sb.AppendLine($"Error processing PDFs:\r\n{e.GetType().FullName}\r\n{e.Message}\r\nPossible culprit: {lastExaminedPdfFile}\r\n\r\n{e.StackTrace}\r\n\r\n");
                    exc = exc.InnerException;
                }
                return HttpResponse.PlainText(sb.ToString(), HttpStatusCode._500_InternalServerError);
            }
        }
    }
}