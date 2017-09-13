using RT.PropellerApi;
using RT.Util;

namespace KtaneWeb
{
    class Program
    {
        static void Main(string[] args)
        {
            PropellerUtil.RunStandalone(PathUtil.AppPathCombine("KTANE-Propeller-standalone.json"), new KtanePropellerModule());
        }
    }
}
