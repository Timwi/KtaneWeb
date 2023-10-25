using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RT.CommandLine;
using RT.PostBuild;
using RT.PropellerApi;
using RT.Serialization;
using RT.Util;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace KtaneWeb
{
    [CommandLine]
    abstract class CommandLineBase
    {
        public abstract int Execute();

        public static void PostBuildCheck(IPostBuildReporter rep)
        {
            CommandLineParser.PostBuildStep<CommandLineBase>(rep, null);
        }
    }

    [CommandName("run"), Documentation("Runs a standalone KtaneWeb server.")]
    sealed class Run : CommandLineBase
    {
        [IsPositional]
        public string ConfigFile = null;

        public override int Execute()
        {
            PropellerUtil.RunStandalone(ConfigFile ?? PathUtil.AppPathCombine("KTANE-Propeller-standalone.json"), new KtanePropellerModule(),
#if DEBUG
                propagateExceptions: true
#else
                propagateExceptions: false
#endif
            );
            return 0;
        }
    }

    [CommandName("postbuild"), Undocumented]
    sealed class PostBuild : CommandLineBase
    {
        [IsPositional, IsMandatory, Undocumented]
        public string SourcePath = null;

        public override int Execute() => PostBuildChecker.RunPostBuildChecks(SourcePath, Assembly.GetExecutingAssembly());
    }

    abstract class CommandWithConfig : CommandLineBase
    {
        [IsPositional, IsMandatory, Documentation("KtaneWeb configuration file (JSON).")]
        public string ConfigFile;

        public override int Execute()
        {
            if (string.IsNullOrWhiteSpace(ConfigFile) || !File.Exists(ConfigFile))
            {
                ConsoleUtil.WriteLine($"The specified configuration file, {ConfigFile.Color(ConsoleColor.Yellow)}, does not exist.", null);
                return 1;
            }
            Console.WriteLine($"Using config file: {ConfigFile}");

            var config = ClassifyJson.DeserializeFile<KtaneWebConfig>(ConfigFile);
            return execute(config);
        }

        protected abstract int execute(KtaneWebConfig config);
    }

    [CommandName("cleanuplogfiles"), Documentation("Runs the clean-up task which moves some logfiles to 7z archives.")]
    sealed class CleanUpLogfiles : CommandWithConfig, ICommandLineValidatable
    {
        [Option("-p", "--prefix"), Documentation("Specifies a prefix (two hexadecimal digits) of which logfiles to clean up. The default is to derive a prefix from the current day (1-16) and hour (0-15), so an hourly scheduled task will deal with all logfiles throughout the first 16 days of each month.")]
        public string Prefix;

        [Option("-a", "--augmented"), Documentation("Use augmented output when invoking 7z.")]
        public bool Augmented;

        public ConsoleColoredString Validate() => Prefix == null || Regex.IsMatch(Prefix.ToLowerInvariant(), "^[0-9a-f]{2}$") ? null : "Invalid prefix.".Color(ConsoleColor.Magenta);

        protected override int execute(KtaneWebConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Archive7zPath))
            {
                ConsoleUtil.WriteLine($"7z path is not configured.".Color(ConsoleColor.Magenta));
                return 1;
            }

            var prefix = Prefix?.ToLowerInvariant();
            if (Prefix == null)
            {
                var day = DateTime.UtcNow.Day;
                var dayOffset = 1;
                if (day >= 16 + dayOffset || day < dayOffset)
                {
                    Console.WriteLine($"Nothing to do on day {day}.");
                    return 0;
                }
                var hour = DateTime.UtcNow.Hour;
                if (hour >= 16 || hour < 0)
                {
                    Console.WriteLine($"Nothing to do on hour {hour}.");
                    return 0;
                }

                prefix = $"{day - dayOffset:X1}{hour:X1}";
            }
            Console.WriteLine($"Processing prefix {prefix}.".Color(ConsoleColor.Yellow));

            // List of logfiles that exist as raw files, alongside their date/time
            var existingLogs = new DirectoryInfo(config.LogfilesDir).GetFiles($"{prefix}*.txt").Select(f => (name: f.Name, lastChange: File.GetLastAccessTimeUtc(f.FullName))).ToArray();
            // List of logfiles that are already in the 7z archive
            var archivePath = Path.Combine(config.LogfilesDir, $"Ktane Logfiles {prefix}.7z");
            var (archivedLogs, numBlocks) = GetFileList(config, archivePath, Augmented);

            // Logfiles that are not yet in the 7z file — add them if there are a few
            var missingFromArchive = existingLogs.Select(tup => tup.name).Except(archivedLogs).ToArray();
            if (missingFromArchive.Length >= 8)
            {
                ConsoleUtil.WriteLine($"Adding {missingFromArchive.Length} files to archive {archivePath}".Color(ConsoleColor.Green));
                var tempListFile = Path.Combine(config.LogfilesDir, "files");
                File.WriteAllLines(tempListFile, missingFromArchive.Select(f => Path.Combine(config.LogfilesDir, f)));
                var exit = CommandRunner.Run(config.Archive7zPath, "a", archivePath, "@" + tempListFile).Apply(c => Augmented ? c.OutputAugmented() : c.OutputNothing()).GoGetExitCode();
                File.Delete(tempListFile);
                if (exit != 0)
                {
                    ConsoleUtil.WriteLine($"Aborting because 7z a (add files to archive {archivePath}) reported exit code {exit}.".Color(ConsoleColor.Magenta));
                    return exit;
                }

                // Make absolutely sure that the logfiles are all in the archive now
                var (archivedLogs2, numBlocks2) = GetFileList(config, archivePath, Augmented);
                if (missingFromArchive.Except(archivedLogs2).FirstOrDefault() is string firstMissing)
                {
                    ConsoleUtil.WriteLine($"I tried to add logfiles to archive {archivePath} but 7z l (list archive) isn’t showing {firstMissing}.".Color(ConsoleColor.Magenta));
                    return 1;
                }

                // If all these tests pass, we can delete logfiles older than 30 days
                var deleted = 0;
                foreach (var (name, lastChange) in existingLogs)
                    if ((DateTime.UtcNow - lastChange).TotalDays >= 30)
                    {
                        deleted++;
                        File.Delete(Path.Combine(config.LogfilesDir, name));
                    }
                ConsoleUtil.WriteLine($"Deleted {deleted} files.".Color(ConsoleColor.Cyan));
            }
            else
                ConsoleUtil.WriteLine($"There are only {missingFromArchive.Length} files not in the archive {archivePath} — not doing it yet".Color(ConsoleColor.Green));

            return 0;
        }

        private static (List<string> archivedLogs, int numBlocks) GetFileList(KtaneWebConfig config, string archivePath, bool augmented)
        {
            if (!File.Exists(archivePath))
                return (new List<string>(), 0);

            var lines = CommandRunner.Run(config.Archive7zPath, "l", archivePath).Apply(c => augmented ? c.OutputAugmented() : c.OutputNothing()).GoGetOutputText().Split('\n');
            var files = new List<string>();
            var numBlocks = 0;
            foreach (var row in lines)
            {
                var m = Regex.Match(row, @"^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d ..... +\d+(?: +\d+)? +([0-9a-f]{40}\.txt)\s*$");
                if (m.Success)
                    files.Add(m.Groups[1].Value);
                else
                {
                    m = Regex.Match(row, @"^Blocks = (\d+)\s*$");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var nb))
                        numBlocks = nb;
                }
            }
            return (files, numBlocks);
        }
    }

    [CommandName("cleanuppdfs"), Documentation("Runs the clean-up task which deletes old PDF files.")]
    sealed class CleanUpPdfs : CommandWithConfig
    {
        protected override int execute(KtaneWebConfig config)
        {
            // Delete merged PDFs that are older than 24 hours
            foreach (var file in new DirectoryInfo(config.MergedPdfsDir).GetFiles("*.pdf"))
                if ((DateTime.UtcNow - File.GetLastWriteTimeUtc(file.FullName)).TotalHours > 24)
                    File.Delete(file.FullName);

            // Delete PDFs that are older than 24 hours
            foreach (var file in new DirectoryInfo(config.PdfTempPath).GetFiles("*.pdf"))
                if ((DateTime.UtcNow - File.GetLastWriteTimeUtc(file.FullName)).TotalHours > 24)
                    File.Delete(file.FullName);

            return 0;
        }
    }
}
