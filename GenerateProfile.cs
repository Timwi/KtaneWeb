using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using RT.Json;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse generateProfileZip(HttpRequest req)
        {
            var moduleInfoCache = _moduleInfoCache;
            byte[] generateProfile(int operation, Func<KtaneModuleInfo, bool> filter)
            {
                var jsonList = moduleInfoCache.Modules.Where(k => k.ModuleID != null && (k.Type == KtaneModuleType.Regular || k.Type == KtaneModuleType.Needy) && filter(k)).Select(k => k.ModuleID).ToJsonList();
                return new JsonDict { { operation == 0 ? "EnabledList" : "DisabledList", jsonList }, { "Operation", operation } }.ToString().ToUtf8();
            }

            using var mem = new MemoryStream();
            var zipFile = new ZipArchive(mem, ZipArchiveMode.Create, leaveOpen: true);

            void addEntry(byte[] data, string entryName)
            {
                var entry = zipFile.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(data, 0, data.Length);
            }

            foreach (var difficulty in EnumStrong.GetValues<KtaneModuleDifficulty>())
            {
                addEntry(generateProfile(1, k => k.DefuserDifficulty == difficulty), @"Veto defuser {0}.json".Fmt(difficulty.ToReadable()));
                addEntry(generateProfile(0, k => k.ExpertDifficulty == difficulty), @"Expert {0}.json".Fmt(difficulty.ToReadable()));
            }
            addEntry(generateProfile(1, k => k.BossStatus == KtaneBossStatus.FullBoss), @"Veto full boss modules.json");
            addEntry(generateProfile(1, k => k.BossStatus == KtaneBossStatus.SemiBoss), @"Veto semi-boss modules.json");
            addEntry(generateProfile(1, k => k.Quirks.HasFlag(KtaneQuirk.PseudoNeedy)), @"Veto pseudo-needy modules.json");
            addEntry(generateProfile(1, k => k.Quirks.HasFlag(KtaneQuirk.TimeDependent)), @"Veto heavily time-dependent modules.json");
            addEntry(generateProfile(1, k => k.RuleSeedSupport != KtaneSupport.Supported), @"Only rule-seeded.json");
            addEntry(generateProfile(1, k => k.Souvenir == null || k.Souvenir.Status != KtaneModuleSouvenir.Supported), @"Only Souvenir supported.json");
            addEntry(generateProfile(1, k => k.Issues != KtaneModuleIssues.None), @"Veto modules with issues.json");

            return HttpResponse.Create(mem.ToArray(), "application/octet-stream", headers: new HttpResponseHeaders
            {
                ContentDisposition = new HttpContentDisposition
                {
                    Mode = HttpContentDispositionMode.Attachment,
                    Filename = @"""Difficulty-based profiles.zip"""
                }
            });
        }
    }
}
