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

        private HttpResponse generateProfileZip(HttpRequest req)
        {
            byte[] generateProfile(int operation, Func<KtaneModuleInfo, bool> filter)
            {
                var jsonList = _moduleInfoCache.Modules.Where(k => k.ModuleID != null && (k.Type == KtaneModuleType.Regular || k.Type == KtaneModuleType.Needy) && filter(k)).Select(k => k.ModuleID).ToJsonList();
                return new JsonDict { { "DisabledList", jsonList }, { "Operation", operation } }.ToString().ToUtf8();
            }

            return new UrlResolver(
                new UrlMapping(path: "/zip", handler: rq =>
                {
                    using (var mem = new MemoryStream())
                    {
                        var zipFile = ZipFile.Create(mem);
                        zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                        foreach (var difficulty in EnumStrong.GetValues<KtaneModuleDifficulty>())
                        {
                            zipFile.Add(new InMemoryDataSource(generateProfile(1, k => k.DefuserDifficulty == difficulty)), @"Veto defuser {0}.json".Fmt(difficulty.ToReadable()), CompressionMethod.Deflated, true);
                            zipFile.Add(new InMemoryDataSource(generateProfile(0, k => k.ExpertDifficulty != difficulty)), @"+ Expert {0}.json".Fmt(difficulty.ToReadable()), CompressionMethod.Deflated, true);
                        }
                        zipFile.Add(new InMemoryDataSource(generateProfile(1, k => k.RuleSeedSupport != KtaneSupport.Supported)), @"Only rule-seeded.json", CompressionMethod.Deflated, true);
                        zipFile.Add(new InMemoryDataSource(generateProfile(1, k => k.TwitchPlaysSupport != KtaneSupport.Supported)), @"Only Twitch Plays supported.json", CompressionMethod.Deflated, true);
                        zipFile.Add(new InMemoryDataSource(generateProfile(1, k => k.Souvenir == null || k.Souvenir.Status != KtaneModuleSouvenir.Supported)), @"Only Souvenir supported.json", CompressionMethod.Deflated, true);
                        zipFile.Add(new InMemoryDataSource(generateProfile(1, k => k.Compatibility != KtaneModuleCompatibility.Compatible)), @"Veto incompatible.json", CompressionMethod.Deflated, true);
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
