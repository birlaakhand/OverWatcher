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
            ICEExceptionBalmoFilter(ice);
            ICEExceptionLNGFilter(ice);
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
            AddCount("LNG Futures", count);
            return ice;
        }
        public DataTable ICEExceptionLNGFilter(DataTable ice)
        {
            int count = 0;
            for(int i = 0; i < ice.Rows.Count; i++)
            {
                if("LNG Futures" == ice.Rows[i]["Product"].ToString())
                {
                    ice.Rows.RemoveAt(i);
                    count++;
                }
            }
            AddCount("LNG Futures", count);
            return ice;
        }

        public DataTable ICEExceptionBalmoFilter(DataTable ice)
        {
            int count = 0;
            for (int i = 0; i < ice.Rows.Count; i++)
            {
                if ("LNG Futures" == ice.Rows[i]["Product"].ToString())
                {
                    ice.Rows.RemoveAt(i);
                    count++;
                }
            }
            AddCount("Balmo", count);
            return ice;
        }

        public override void Filter(List<DataTable> dt)
        {
        }
    }
}
