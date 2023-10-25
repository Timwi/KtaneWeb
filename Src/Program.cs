using RT.CommandLine;

namespace KtaneWeb
{
    class Program
    {
        static int Main(string[] args)
        {
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
        }
    }
}
