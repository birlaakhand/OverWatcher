using OverWatcher.Common.HelperFunctions;
using OverWatcher.TradeReconMonitor.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    class CrossTableFilter : DataTableFilterBase
    {
        private delegate void ResultFilter(List<DataTable> diff);
        private List<ResultFilter> filterList = new List<ResultFilter>();

        public override void Filter(List<DataTable> diff)
        {
            filterList.Add(DiffFilter);
            foreach(ResultFilter r in filterList)
            {
                r.Invoke(diff);
            }
        }

        public override void Filter(DataTable dt)
        {
        }

        private void DiffFilter(List<DataTable> diff)
        {
            foreach (CompanyName c in Enum.GetValues(typeof(CompanyName)))
            {
                var tables = diff.FindAll(d => d.TableName.Contains(c.ToString()));
                DataTable swap = tables.Find(d => d.TableName.Contains(ProductType.Swap.ToString()));
                DataTable future = tables.Find(d => d.TableName.Contains(ProductType.Futures.ToString()));
                if (swap == null || future == null) return;
                HelperFunctions.SortDataTable<int>(swap, "Deal ID", SortDirection.ASC);
                HelperFunctions.SortDataTable<int>(future, "Deal ID", SortDirection.ASC);
                int swapCount = 0;
                int futureCount = 0;
                LinkedList<DataRow> swapRemoveList = new LinkedList<DataRow>();
                LinkedList<DataRow> futureRemoveList = new LinkedList<DataRow>();
                while (swapCount < swap.Rows.Count && futureCount < future.Rows.Count)
                {
                    int swapID = 0;
                    int futureID = 0;
                    int.TryParse(swap.Rows[swapCount]["Deal ID"].ToString(), out swapID);
                    int.TryParse(future.Rows[futureCount]["Deal ID"].ToString(), out futureID);
                    if (swapID != futureID)
                    {
                        if (swapID > futureID)
                        {
                            ++futureCount;
                        }
                        else
                        {
                            ++swapCount;
                        }
                        continue;
                    }
                    if (swap.Rows[swapCount]["B/S"] != future.Rows[futureCount]["B/S"] &&
                        swap.Rows[swapCount]["Contract"] == future.Rows[futureCount]["Contract"] &&
                        swap.Rows[swapCount]["Lots"] == future.Rows[futureCount]["Lots"] &&
                        swap.Rows[swapCount]["Trader"] == future.Rows[futureCount]["Trader"] &&
                        swap.Rows[swapCount]["Product"].ToString().Length > future.Rows[futureCount]["Product"].ToString().Length ?
                            swap.Rows[swapCount]["Product"].ToString().Contains(future.Rows[futureCount]["Product"].ToString()) :
                            future.Rows[futureCount]["Product"].ToString().Contains(swap.Rows[swapCount]["Product"].ToString()))
                    {
                        swapRemoveList.AddLast(swap.Rows[swapCount]);
                        futureRemoveList.AddLast(future.Rows[futureCount]);
                        ++futureCount;
                        ++swapCount;
                        AddCount("EFS Trade", 1);
                    }
                }
                foreach (var dataRow in swapRemoveList)
                {
                    swap.Rows.Remove(dataRow);
                }
                foreach (var dataRow in futureRemoveList)
                {
                    future.Rows.Remove(dataRow);
                }
            }

        }
    }
}
