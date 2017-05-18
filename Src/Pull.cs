using System.Text;
using RT.Servers;
using RT.Util;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse pull(KtaneWebConfigEntry config)
        {
            var cmd = new CommandRunner();
            cmd.Command = "git pull";
            cmd.WorkingDirectory = config.BaseDir;
            var output = new StringBuilder();
            cmd.StdoutText += str => output.Append(str);
            cmd.StderrText += str => output.Append(str);
            cmd.StartAndWait();
            return HttpResponse.PlainText(output.ToString());
        }
    }
}
