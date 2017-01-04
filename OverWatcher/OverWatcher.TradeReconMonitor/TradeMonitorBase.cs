﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    class TradeMonitorBase : COMManagerBase
    {
        public int Futures { get; protected set; }
        public int Swap { get; protected set; }
        protected string MonitorTitle = "Base";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected TradeMonitorBase(string title)
        {
            MonitorTitle = title;
        }
        private TradeMonitorBase() { }
        public string CountToHTML()
        {

            return HTMLGenerator.CountToHTML(MonitorTitle, Swap, Futures);
        }

        public void LogCount()
        {
            log.Info(MonitorTitle + " Future count:" + Futures
                                + "   "
                                + "Cleared count:" + Swap);
        }
        
    }
}
