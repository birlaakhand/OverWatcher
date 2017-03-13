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
        public abstract void Filter(DataTable dt);
        public abstract void Filter(List<DataTable> dt);
        protected Dictionary<string, int> ExcludedCountMap
        {
            get;
        } = new Dictionary<string, int>();

        protected void AddCount(string key, int count)
        {
            if (!ExcludedCountMap.Keys.Contains(key))
            {
                ExcludedCountMap[key] = count;
            }
            else
            {
                ExcludedCountMap[key] = ExcludedCountMap[key] + count;
            }
        }

        public string CountString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var tuple in ExcludedCountMap)
                {
                    sb.AppendLine(tuple.Value.ToString() + " of "
                                + tuple.Key + " has excluded; ");
                }
                return sb.ToString();
            }
        }
    }
}
