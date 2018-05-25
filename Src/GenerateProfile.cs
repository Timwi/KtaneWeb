using System;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        sealed class InMemoryDataSource : IStaticDataSource
        {
            public byte[] Data { get; private set; }
            public InMemoryDataSource(byte[] data) { Data = data; }
            public Stream GetSource() => new MemoryStream(Data);
        }

        private HttpResponse generateProfile(HttpRequest req)
        {
            byte[] generateDefExp(KtaneModuleDifficulty desired, int operation, Func<KtaneModuleInfo, bool> filter)
            {
                var jsonList = _config.Current.KtaneModules.Where(k => k.ModuleID != null && (k.Type == KtaneModuleType.Regular || k.Type == KtaneModuleType.Needy) && filter(k)).Select(k => k.ModuleID).ToJsonList();
                return new JsonDict { { "DisabledList", jsonList }, { "Operation", operation } }.ToString().ToUtf8();
            }

            HttpResponse generateDefExpResponse(HttpRequest rq, int operation, string name, Func<KtaneModuleInfo, KtaneModuleDifficulty, bool> filter)
            {
                var desired = EnumStrong.Parse<KtaneModuleDifficulty>(rq.Url.Path.Substring(1));
                return HttpResponse.Create(
                    generateDefExp(desired, operation, k => filter(k, desired)),
                    "application/octet-stream",
                    headers: new HttpResponseHeaders { ContentDisposition = new HttpContentDisposition { Mode = HttpContentDispositionMode.Attachment, Filename = name.Fmt(desired.ToReadable()) } });
            }

            return new UrlResolver(
                new UrlMapping(path: "/defuser", handler: rq => generateDefExpResponse(rq, 1, @"""Veto defuser {0}.json""", (k, d) => k.DefuserDifficulty == d)),
                new UrlMapping(path: "/expert", handler: rq => generateDefExpResponse(rq, 0, @"""+ Expert {0}.json""", (k, d) => k.ExpertDifficulty != d)),
                new UrlMapping(path: "/zip", handler: rq =>
                {
                    using (var mem = new MemoryStream())
                    {
                        var zipFile = ZipFile.Create(mem);
                        zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                        foreach (var difficulty in EnumStrong.GetValues<KtaneModuleDifficulty>())
                        {
                            zipFile.Add(new InMemoryDataSource(generateDefExp(difficulty, 1, k => k.DefuserDifficulty == difficulty)), @"Veto defuser {0}.json".Fmt(difficulty.ToReadable()), CompressionMethod.Deflated, true);
                            zipFile.Add(new InMemoryDataSource(generateDefExp(difficulty, 0, k => k.ExpertDifficulty == difficulty)), @"+ Expert {0}.json".Fmt(difficulty.ToReadable()), CompressionMethod.Deflated, true);
                        }
                        zipFile.CommitUpdate();
                        zipFile.Close();
                        return HttpResponse.Create(mem.ToArray(), "application/octet-stream",
                             headers: new HttpResponseHeaders { ContentDisposition = new HttpContentDisposition { Mode = HttpContentDispositionMode.Attachment, Filename = @"""Difficulty-based profiles.zip""" } });
                    }
                })
            ).Handle(req);
        }
    }
}
