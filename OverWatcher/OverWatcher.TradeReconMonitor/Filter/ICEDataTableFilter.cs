using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TradeReconMonitor.Core
{
    class ICEDataTableFilter : DataTableFilterBase
    {
        public override void Filter(DataTable ice)
        {
            ICELinkIDFilter(ice);
            foreach (string product in System.Configuration
                .ConfigurationManager
                .AppSettings["ExcludedProduct"]
                .Split(";".ToCharArray()))
            {
                ICEProductExceptionFilter(product, ice);
            }
        }
        public DataTable ICELinkIDFilter(DataTable ice)
        {
            int count = 0;
            LinkedList<DataRow> repeated = new LinkedList<DataRow>();
            int loop = 2;
            do
            {
                for (int i = 0; i < ice.Rows.Count; i++)
                {
                    if (string.IsNullOrEmpty(ice.Rows[i]["Deal ID"]?.ToString()))
                        continue;
                    var match = repeated.Any(x => ice.Rows[i]["Deal ID"].ToString() ==
                                                 x["Link ID"].ToString()) ? ice.Rows[i] : null;
                    if (match != null)
                    {
                        ice.Rows.Remove(match);
                        repeated.Remove(match);
                        count++;
                        continue;
                    }
                    if (!string.IsNullOrEmpty(ice.Rows[i]["Link ID"]?.ToString()))
                    {
                        repeated.AddLast(ice.Rows[i]);
                    }
                }
            } while (--loop > 0);
            AddCount("Link ID", count);
            return ice;
        }
        public DataTable ICEProductExceptionFilter(string product, DataTable ice)
        {
            int count = 0;
            for(int i = 0; i < ice.Rows.Count; i++)
            {
                if(product == ice.Rows[i]["Product"].ToString())
                {
                    ice.Rows.RemoveAt(i);
                    count++;
                }
            }
            AddCount(product, count);
            return ice;
        }

        public override void Filter(List<DataTable> dt)
        {
        }
    }
}
