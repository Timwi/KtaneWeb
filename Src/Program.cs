using RT.CommandLine;

namespace KtaneWeb
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
                args = new[] { "run" };

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
