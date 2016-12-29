using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher
{
    class TradeMonitorBase
    {
        protected int futures = 0;
        protected int swap = 0;
        protected string MonitorTitle = "Base";
        protected TradeMonitorBase(string title)
        {
            MonitorTitle = title;
        }
        private TradeMonitorBase() { }
        public string CountToHTML()
        {

            return HTMLGenerator.CountToHTML(MonitorTitle, swap, futures);
        }

        public void LogCount()
        {
            Console.WriteLine(MonitorTitle + "   Count:" 
                                + Environment.NewLine
                                + "Future count:" + futures
                                + Environment.NewLine
                                + "Cleared count:" + swap);
        }
    }
}
