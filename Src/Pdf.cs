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
        private HttpResponse pdfOrFileSystem(HttpRequest req)
        {
            if (!req.Url.Path.StartsWith("/PDF/", StringComparison.InvariantCultureIgnoreCase))
                goto doFileSystem;

            var filename = req.Url.Path.Substring(5);
            if (filename.Length < 1 || filename.Contains('/'))
                goto doFileSystem;
            filename = filename.UrlUnescape();

            // If the PDF file already exists in the PDF folder, use that
            if (File.Exists(Path.Combine(_config.BaseDir, "PDF", filename)))
                goto doFileSystem;

            // See if an equivalent HTML file exists, even with a wildcard match or incorrect filename capitilization
            string htmlFile = new DirectoryInfo(Path.Combine(_config.BaseDir, "HTML")).GetFiles(Path.GetFileNameWithoutExtension(filename) + ".html").Select(fs => fs.FullName).FirstOrDefault();
            if (htmlFile == null)
                goto doFileSystem;

            // Check if the PDF filename is exactly correct and redirect if it isn’t
            string pdfUrl = $"/PDF/{Path.GetFileNameWithoutExtension(htmlFile)}.pdf";
            if (!Regex.IsMatch(pdfUrl, $"^{Regex.Escape("/PDF/" + filename).Replace("\\*", ".*")}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                goto doFileSystem;
            if (pdfUrl != req.Url.Path.Substring(0, 5) + filename)
                return HttpResponse.Redirect(req.Url.WithPath(pdfUrl));

            // Turns out an HTML file corresponding to the requested PDF file exists, so we will try to generate the PDF automatically by invoking Google Chrome
            using (var md5 = MD5.Create())
            {
                var tempFilename = $"{md5.ComputeHash(File.ReadAllBytes(htmlFile)).ToHex()}.pdf";
                var tempFilepath = Path.Combine(_config.PdfTempPath ?? Path.GetTempPath(), tempFilename);
                if (!File.Exists(tempFilepath))
                {
                    var runner = new CommandRunner();
                    runner.Command = $@"cmd.exe /S /C """"{_config.ChromePath}"" --headless --disable-gpu ""--print-to-pdf={tempFilepath}"" --no-margins ""{htmlFile}""""";
                    runner.StartAndWait();
                }
                else
                    File.SetLastAccessTimeUtc(tempFilepath, DateTime.UtcNow);

                if (_config.PdfTempPath != null && Rnd.Next(0, 100) == 0)
                {
                    // Clean up older PDF files
                    foreach (var file in new DirectoryInfo(_config.PdfTempPath).EnumerateFiles("*.pdf"))
                        if ((DateTime.UtcNow - File.GetLastAccessTimeUtc(file.FullName)).TotalDays > 7)
                            file.Delete();
                }

                return HttpResponse.File(tempFilepath, "application/pdf");
            }

            doFileSystem:
            return new FileSystemHandler(_config.BaseDir, new FileSystemOptions { MaxAge = null }).Handle(req);
        }

        private HttpResponse mergePdfs(HttpRequest req)
        {
            ensureModuleInfoCache();

            string lastExaminedPdfFile = "<none>";
            try
            {
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