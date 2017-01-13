using OverWatcher.Common.Logging;

namespace OverWatcher.TradeReconMonitor.Core
{
    interface ITradeMonitor
    {
        int Futures { get; }
        int Swap { get; }
        string MonitorTitle { get;}
        string CountToHTML();

        void LogCount();
        
    }
}
