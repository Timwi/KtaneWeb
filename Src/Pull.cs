using System;
using System.Text;
using RT.Servers;
using RT.Util;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private bool _pullActive = false;

        private HttpResponse pull(HttpRequest req)
        {
            if (_pullActive)
                return HttpResponse.PlainText("A pull is already currently active.");

            try
            {
                _pullActive = true;
                var output = new StringBuilder();
                var runGitPull = req.Url["dont"] != "1";
                if (runGitPull)
                {
                    output.Append("git fetch origin\n==============================\n");
                    var cmd = new CommandRunner
                    {
                        Command = "git fetch origin",
                        WorkingDirectory = _config.BaseDir
                    };
                    cmd.StdoutText += str => output.Append(str);
                    cmd.StderrText += str => output.Append(str);
                    cmd.StartAndWait();

                    output.Append("\n\ngit reset --hard origin/master\n==============================\n");
                    cmd = new CommandRunner
                    {
                        Command = "git reset --hard origin/master",
                        WorkingDirectory = _config.BaseDir
                    };
                    cmd.StdoutText += str => output.Append(str);
                    cmd.StderrText += str => output.Append(str);
                    cmd.StartAndWait();
                }
                generateTranslationCache();
                generateModuleInfoCache();
                return HttpResponse.PlainText(runGitPull ? output.ToString() : "Module info refreshed.");
            }
            catch (Exception e)
            {
                return HttpResponse.PlainText($"{e.Message}\n\n{e.GetType().FullName}");
            }
            finally
            {
                _pullActive = false;
            }
        }
    }
}
