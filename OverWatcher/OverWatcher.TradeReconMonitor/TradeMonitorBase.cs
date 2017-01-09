using OverWatcher.Common.Log;

namespace OverWatcher.TradeReconMonitor.Core
{
    class TradeMonitorBase : COMManagerBase
    {
        public int Futures { get; protected set; }
        public int Swap { get; protected set; }
        protected string MonitorTitle = "Base";
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
            Logger.Info(MonitorTitle + " Future count:" + Futures
                                + "   "
                                + "Cleared count:" + Swap);
        }
        
    }
}
