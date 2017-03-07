using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    abstract class DataTableFilterBase
    {
        public abstract DataTable Filter(DataTable dt);
        public Dictionary<string, int> ExcludedCountMap
        {
            get;
        } = new Dictionary<string, int>();

        public string CountString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var tuple in ExcludedCountMap)
                {
                    sb.Append(tuple.Value.ToString() + " of "
                                + tuple.Key + "has excluded; ");
                }
                return sb.ToString();
            }
        }
    }
}
