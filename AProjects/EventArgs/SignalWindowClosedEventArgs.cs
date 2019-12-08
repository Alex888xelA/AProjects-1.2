using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AProjects
{
    class SignalWindowClosedEventArgs : EventArgs
    {
        private String msg;

        public SignalWindowClosedEventArgs(String s)
        {
            msg = s;
        }

        public String Message
        {
            get { return msg; }
        }
    }
}
