using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KtaneWeb
{
    sealed class SelectableData
    {
        public string HumanReadable;
        public string DataAttributeName;
        public Func<KtaneModuleInfo, string> DataAttributeValue;
    }
}
