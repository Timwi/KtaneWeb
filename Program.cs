using KtaneWeb;
using RT.CommandLine;

if (args.Length == 0)
    args = ["run"];

CommandLine cmd;
try
{
    cmd = CommandLineParser.Parse<CommandLine>(args);
}
catch (CommandLineParseException pe)
{
    pe.WriteUsageInfoToConsole();
    return 1;
}

return cmd.Execute();
