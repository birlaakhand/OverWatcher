using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.TheICETrade
{
    class DataTableComparator
    {

        public List<DataTable> Diff(List<DataTable> iceList, List<DataTable> oracleList)
        {
            Console.WriteLine("Diff ICE and Oracle...");
            List<DataTable> diff = new List<DataTable>();
            MergeLegIDAndDealID(iceList);
            foreach(CompanyName c in Enum.GetValues(typeof(CompanyName)))
            {
                foreach(ProductType p in Enum.GetValues(typeof(ProductType)))
                {
                    DataTable ice = iceList.Find(s => s.TableName == c.ToString() + p.ToString());
                    DataTable oracle = oracleList.Find(s => s.TableName == c.ToString() + p.ToString());
                    diff.Add(Diff(ice, oracle));
                }
            }
            return diff;
        }

        public DataTable Diff(DataTable ice, DataTable oracle) //sort and diff
        {
            HelperFunctions.SortDataTable(ice, "Deal ID", SortDirection.ASC);
            HelperFunctions.SortDataTable(oracle, "ICEDEALID", SortDirection.ASC);
            DataTable diff = ice.Clone();
            int oracleCount = 0;
            int iceCount = 0;
            for(oracleCount = 0; oracleCount < oracle.Rows.Count; ++oracleCount)
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
                    diff.Rows.Add(ice.Rows[iceCount]);
                    iceCount++;
                }
                else
                {
                    diff.Rows.Add(ice.Rows[iceCount]);
                    oracleCount++;
                }
            }
            return diff;
        }

        public void MergeLegIDAndDealID(List<DataTable> iceList)
        {
            iceList = iceList.Select(t =>
            {
                foreach (DataRow row in t.Rows)
                {
                    if (row["Leg ID"] != DBNull.Value || !string.IsNullOrEmpty(row["Leg ID"].ToString()))
                    {
                        row["Deal ID"] = row["Leg ID"];
                    }
                }
                return t;
            }).ToList();
        }
    }
}
