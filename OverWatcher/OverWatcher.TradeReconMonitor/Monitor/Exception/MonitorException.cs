using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    class OracleQueryAbortException : Exception
    {
        public OracleQueryAbortException(string msg) : base(msg) { }
    }

    class MonitorException : Exception
    {
        public MonitorException(string msg) : base(msg) { }
        public MonitorException(string msg, Exception ex) : base(msg, ex) { }
    }
}
