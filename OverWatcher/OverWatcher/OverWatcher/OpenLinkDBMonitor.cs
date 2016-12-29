using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TheICETrade
{
    class OracleDBMonitor : TradeMonitorBase
    {
        public OracleDBMonitor() : base("OpenLinkTrade") { }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public List<DataTable> QueryDB()
        {
            log.Info("Query the Oracle Database..");
            var dtList = new List<DataTable>();
            using (DBConnector db = new DBConnector())
            {
                DisposableCleaner.ManageDisposable(db);
                foreach (CompanyName company in Enum.GetValues(typeof(CompanyName)))
                {
                    foreach (ProductType product in Enum.GetValues(typeof(ProductType)))
                    {
                        string name = company.ToString() + product.ToString();
                        dtList.Add(db.MakeQuery(ConfigurationManager.AppSettings[name + "Query"], name));
                        if(product == ProductType.Futures)
                        {
                            futures += dtList.Last().Rows.Count;
                        }
                        else
                        {
                            swap += dtList.Last().Rows.Count;
                        }
                    }
                }
            }
            return dtList;
        }
    }
}
