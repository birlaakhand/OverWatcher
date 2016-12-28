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

        #region Comparsion
        public DataTable CompareDataTable(DataTable t1, DataTable t2)
        {
            return null;
        }
        public int CompareCount(DataTable t1, DataTable t2)
        {
            return 0;
        }
        public void Compare(List<DataTable> lt1, List<DataTable> lit2)
        {

        }
        #endregion

        public void MergeLegIDAndDealID(List<DataTable> lt1)
        {
            lt1 = lt1.Select(t =>
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
