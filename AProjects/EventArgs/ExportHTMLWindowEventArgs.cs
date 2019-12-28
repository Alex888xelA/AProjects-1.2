using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AProjects
{
    class ExportHTMLWindowEventArgs : EventArgs
    {
        private Dictionary<String, Boolean> settings;

        public ExportHTMLWindowEventArgs(Dictionary<String, Boolean> s)
        {
            settings = s;
        }

        public Dictionary<String, Boolean> Message
        {
            get { return settings; }
        }
    }
}
