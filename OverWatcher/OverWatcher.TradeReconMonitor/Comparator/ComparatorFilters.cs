﻿using OverWatcher.Common.HelperFunctions;
using OverWatcher.TradeReconMonitor.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    partial class ICEOpenLinkComparator
    {
        private delegate void ResultFilter(List<DataTable> diff);
        private List<ResultFilter> filterList = new List<ResultFilter>();

        public void ApplyFilter(List<DataTable> diff)
        {
            filterList.Add(DiffFilter);
            foreach(ResultFilter r in filterList)
            {
                r.Invoke(diff);
            }
        }
        private static void DiffFilter(List<DataTable> diff)
        {
            foreach (CompanyName c in Enum.GetValues(typeof(CompanyName)))
            {
                var tables = diff.FindAll(d => d.TableName.Contains(c.ToString()));
                DataTable swap = tables.Find(d => d.TableName.Contains(ProductType.Swap.ToString()));
                DataTable future = tables.Find(d => d.TableName.Contains(ProductType.Futures.ToString()));
                if (swap == null || future == null) return;
                HelperFunctions.SortDataTable<int>(swap, "Deal ID", SortDirection.ASC);
                HelperFunctions.SortDataTable<int>(future, "Deal ID", SortDirection.ASC);
                int count = 0;
                LinkedList<KeyValuePair<int, int>> filtered = new LinkedList<KeyValuePair<int, int>>();
                while (count < swap.Rows.Count && count < future.Rows.Count)
                {
                    if (swap.Rows[count]["Deal ID"] == future.Rows[count]["Deal ID"] &&
                        swap.Rows[count]["B/S"] != future.Rows[count]["B/S"] &&                     
                        swap.Rows[count]["Contract"] == future.Rows[count]["Contract"] &&
                        swap.Rows[count]["Lots"] == future.Rows[count]["Lots"] &&
                        swap.Rows[count]["Trader"] == future.Rows[count]["Trader"] &&
                        swap.Rows[count]["Product"].ToString().Length > future.Rows[count]["Product"].ToString().Length ?
                            swap.Rows[count]["Product"].ToString().Contains(future.Rows[count]["Product"].ToString()) :
                            future.Rows[count]["Product"].ToString().Contains(swap.Rows[count]["Product"].ToString()))
                    {
                       swap.Rows.Remove(swap.Rows[count]);
                        future.Rows.Remove(future.Rows[count]);
                    }
                    ++count;
                }
            }

        }
    }
}