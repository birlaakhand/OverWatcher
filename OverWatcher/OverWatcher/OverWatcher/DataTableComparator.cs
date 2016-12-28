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

        public void Compare(List<DataTable> ice, List<DataTable> oracle)
        {

        }

        public void MergeLegIDAndDealID(List<DataTable> ice)
        {
            ice = ice.Select(t =>
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
