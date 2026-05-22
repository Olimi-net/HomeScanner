using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <author>
/// Lada 
/// </author>
/// <created>
/// 2019.05.28
/// </created>

namespace Scaner
{
    enum ScanerCmd { Wait = 0, Scan = 1 }

    class ServerArg : EventArgs
    {
        public ScanerCmd Cmd;
        private string Msg;

        public ServerArg(ScanerCmd p)
        {
            this.Cmd = p;
        }

        public ServerArg(ScanerCmd p1, string p2)
        {
            Cmd = p1;
            Msg = p2;
        }

    }
}
