using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TheICETrade
{
    class ICEOpenLinkComparator
    {

        public List<DataTable> Diff(List<DataTable> iceList, List<DataTable> oracleList)
        {
            Console.WriteLine("Diff ICE and Oracle...");
            List<DataTable> diff = new List<DataTable>();
            foreach(DataTable dt in iceList)
            {
                SwapLegIDAndDealID(dt);
            }
            foreach (CompanyName c in Enum.GetValues(typeof(CompanyName)))
            {
                foreach(ProductType p in Enum.GetValues(typeof(ProductType)))
                {
                    DataTable ice = iceList.Find(s => s.TableName == c.ToString() + p.ToString());
                    DataTable oracle = oracleList.Find(s => s.TableName == c.ToString() + p.ToString());
                    diff.Add(SwapLegIDAndDealID(Diff(ice, oracle)));
                }
            }
            foreach (DataTable dt in iceList)
            {
                SwapLegIDAndDealID(dt);
            }
            return diff;
        }

        public DataTable Diff(DataTable ice, DataTable oracle) //sort and diff
        {
            HelperFunctions.SortDataTable(ref ice, "Deal ID", SortDirection.ASC);
            HelperFunctions.SortDataTable(ref oracle, "ICEDEALID", SortDirection.ASC);
            DataTable diff = ice.Clone();
            diff.TableName = diff.TableName + " OpenLink Missing";
            int oracleRowsCount = oracle.Rows.Count;
            int iceRowsCount = ice.Rows.Count;
            int oracleCount = 0;
            int iceCount = 0;
            while (oracleCount < oracleRowsCount)
            {
                int oc = int.Parse(oracle.Rows[oracleCount]["ICEDEALID"].ToString());
                int ic = int.Parse(ice.Rows[iceCount]["Deal ID"].ToString());
                if (oc == ic)
                {
                    oracleCount++;
                    iceCount++;
                }
                else if(oc > ic)
                {
                    diff.ImportRow(ice.Rows[iceCount]);
                    iceCount++;
                }
                else
                {
                    diff.ImportRow(ice.Rows[iceCount]);
                    oracleCount++;
                }
            }
            while(iceCount < iceRowsCount)
            {
                diff.ImportRow(ice.Rows[iceCount]);
                iceCount++;
            }
            return diff;
        }

        public DataTable SwapLegIDAndDealID(DataTable ice)
        {
            foreach (DataRow row in ice.Rows)
            {
                if (row["Leg ID"] != DBNull.Value || !string.IsNullOrEmpty(row["Leg ID"].ToString()))
                {
                    string deal = row["Deal ID"].ToString();
                    string leg = row["Leg ID"].ToString();
                    row["Deal ID"] = leg;
                    row["Leg ID"] = deal;
                }
            }
            return ice;
        }
    }
}
