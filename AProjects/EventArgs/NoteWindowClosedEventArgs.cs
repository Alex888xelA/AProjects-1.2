using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AProjects
{
    class NoteWindowClosedEventArgs : EventArgs
    {
        private String msg;

        public NoteWindowClosedEventArgs(String s)
        {
            msg = s;
        }

        public String Message
        {
            get { return msg; }
        }
    }
}
