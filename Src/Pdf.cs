using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;
using RT.Util.Serialization;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        //private sealed class PdfInfo
        //{
        //    public Dictionary<string, string> preferredManuals;
        //    public string search;
        //    public string[] searchOptions;
        //    public string selectable;
        //    public string sort;
        //}

        private HttpResponse pdf(HttpRequest req)
        {
            return HttpResponse.PlainText("This feature is temporarily disabled because it didn’t work for most people.");

            //if (req.Method != HttpMethod.Post)
            //    return HttpResponse.Redirect(req.Url.WithPathParent().WithPath(""));

            //var json = JsonValue.Parse(req.Post["json"].Value);
            //var pdfs = new List<string>();
            //var selectable = json["filter"]["includeMissing"].GetBool() ? null : getSelectables(getSheets()).Single(s => s.DataAttributeName == json["selectable"].GetString());
            //var keywords = json["search"].GetString().Length == 0 ? null : json["search"].GetString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //var searchOptions = json["searchOptions"].GetList().Select(s => s.GetString()).ToArray();
            //var includeMissing = json["filter"]["includeMissing"].GetBool();
            //var origins = json["filter"]["origin"].GetDict().All(k => !k.Value.GetBool()) ? null : json["filter"]["origin"].GetDict().ToDictionary(k => EnumStrong.Parse<KtaneModuleOrigin>(k.Key), k => k.Value.GetBool());
            //var twitchplays = json["filter"]["twitchplays"].GetDict().All(k => !k.Value.GetBool()) ? null : json["filter"]["twitchplays"].GetDict().ToDictionary(k => EnumStrong.Parse<KtaneTwitchPlays>(k.Key), k => k.Value.GetBool());
            //var types = json["filter"]["type"].GetDict().All(k => !k.Value.GetBool()) ? null : json["filter"]["type"].GetDict().ToDictionary(k => EnumStrong.Parse<KtaneModuleType>(k.Key), k => k.Value.GetBool());

            //// Filter
            //var matchingModules = _config.Current.KtaneModules.Where(m =>
            //{
            //    if (m.DefuserDifficulty != null && ((int) m.DefuserDifficulty.Value < json["filter"]["defdiff"]["min"].GetInt() || (int) m.DefuserDifficulty.Value > json["filter"]["defdiff"]["max"].GetInt()))
            //        return false;
            //    if (m.ExpertDifficulty != null && ((int) m.ExpertDifficulty.Value < json["filter"]["expdiff"]["min"].GetInt() || (int) m.ExpertDifficulty.Value > json["filter"]["expdiff"]["max"].GetInt()))
            //        return false;
            //    if (!includeMissing && selectable.DataAttributeValue(m) == null)
            //        return false;
            //    if (origins != null && !origins[m.Origin])
            //        return false;
            //    if (twitchplays != null && m.TwitchPlaysSupport != null && !twitchplays[m.TwitchPlaysSupport.Value])
            //        return false;
            //    if (types != null && !types[m.Type])
            //        return false;

            //    return keywords == null ||
            //        (searchOptions.Contains("names") && keywords.Any(k => m.Name.ContainsNoCase(k))) ||
            //        (searchOptions.Contains("authors") && keywords.Any(k => m.Author.ContainsNoCase(k))) ||
            //        (searchOptions.Contains("descriptions") && keywords.Any(k => m.Description.ContainsNoCase(k)));
            //});

            //// Sort
            //switch (json["sort"].GetString())
            //{
            //    case "name": matchingModules = matchingModules.OrderBy(m => m.SortKey); break;
            //    case "defdiff": matchingModules = matchingModules.OrderBy(m => m.DefuserDifficulty); break;
            //    case "expdiff": matchingModules = matchingModules.OrderBy(m => m.ExpertDifficulty); break;
            //    case "published": matchingModules = matchingModules.OrderBy(m => m.Published); break;
            //}

            //var pdfFiles = new List<string>();

            //foreach (var module in matchingModules)
            //{
            //    var filename = Path.Combine(_config.BaseDir, _config.PdfDir, module.Name + ".pdf");
            //    if (json["preferredManuals"].ContainsKey(module.Name))
            //    {
            //        var pref = json["preferredManuals"][module.Name].GetString();
            //        var fullFilename = Path.Combine(_config.BaseDir, _config.PdfDir, Regex.Replace(pref, @" \((?:PDF|HTML)\)$", ".pdf"));
            //        if (File.Exists(fullFilename))
            //            filename = fullFilename;
            //    }
            //    if (File.Exists(filename))
            //        pdfFiles.Add(filename);
            //}

            //if (pdfFiles.Count == 0)
            //    return HttpResponse.PlainText("Error: no matching PDF files found.", HttpStatusCode._500_InternalServerError);

            //var mergedPdf = new PdfDocument();
            //foreach (var pdfFile in pdfFiles)
            //{
            //    PdfDocument pdf = PdfReader.Open(pdfFile, PdfDocumentOpenMode.Import);
            //    int count = pdf.PageCount;
            //    for (int idx = 0; idx < count; idx++)
            //        mergedPdf.AddPage(pdf.Pages[idx]);
            //}
            //using (var m = new MemoryStream())
            //{
            //    mergedPdf.Save(m);
            //    return HttpResponse.Create(m.ToArray(), @"application/pdf", HttpStatusCode._200_OK,
            //        new HttpResponseHeaders { ContentDisposition = new HttpContentDisposition { Filename = @"""Merged-manual.pdf""", Mode = HttpContentDispositionMode.Attachment } });
            //}
        }
    }
}
