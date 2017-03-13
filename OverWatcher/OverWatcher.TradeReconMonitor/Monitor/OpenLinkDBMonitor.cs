using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using OverWatcher.Common.Logging;

namespace OverWatcher.TradeReconMonitor.Core
{
    class OracleDBMonitor : ITradeMonitor
    {
        public int Futures { get; private set; }

        public int Swap { get; private set; }

        public string MonitorTitle
        {
            get
            {
                return "OpenLinkTrade";
            }
        }
        public List<DataTable> QueryDB()
        {
            Logger.Info("Query the Oracle Database..");
            var dtList = new List<DataTable>();
            using (DBConnector db = new DBConnector())
            {
                foreach (CompanyName company in Enum.GetValues(typeof(CompanyName)))
                {
                    foreach (ProductType product in Enum.GetValues(typeof(ProductType)))
                    {
                        string name = company.ToString() + product.ToString();
                        dtList.Add(db.MakeQuery(ConfigurationManager.AppSettings[name + "Query"], name));
                        if(product == ProductType.Futures)
                        {
                            Futures += dtList.Last().Rows.Count;
                        }
                        else
                        {
                            Swap += dtList.Last().Rows.Count;
                        }
                    }
                }
            }
            return dtList;
        }

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
