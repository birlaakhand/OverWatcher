using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using OverWatcher.Common.Logging;

namespace OverWatcher.TradeReconMonitor.Core
{
    class OracleDBMonitor : TradeMonitorBase
    {
        public OracleDBMonitor() : base("OpenLinkTrade") { }
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
    }
}
