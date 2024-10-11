using KtaneWeb;
using RT.CommandLine;

if (args.Length == 0)
    args = ["run"];

CommandLineBase cmd;
try
{
    cmd = CommandLineParser.Parse<CommandLineBase>(args);
}
catch (CommandLineParseException pe)
{
    pe.WriteUsageInfoToConsole();
    return 1;
}

return cmd.Execute();
