using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AProjects
{
    class ExportWindowEventArgs : EventArgs
    {
        private Dictionary<String, Boolean> settings;

        public ExportWindowEventArgs(Dictionary<String, Boolean> s)
        {
            settings = s;
        }

        public Dictionary<String, Boolean> Message
        {
            get { return settings; }
        }
    }
}
