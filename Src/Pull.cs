using System.Text;
using RT.Servers;
using RT.Util;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse pull(HttpRequest req)
        {
            var output = new StringBuilder();
            var cmd = new CommandRunner
            {
                Command = "git pull",
                WorkingDirectory = _config.BaseDir
            };
            cmd.StdoutText += str => output.Append(str);
            cmd.StderrText += str => output.Append(str);
            cmd.StartAndWait();
            return HttpResponse.PlainText(output.ToString());
        }
    }
}
