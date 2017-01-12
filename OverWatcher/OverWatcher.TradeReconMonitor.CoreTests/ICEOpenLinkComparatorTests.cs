using Microsoft.VisualStudio.TestTools.UnitTesting;
using OverWatcher.TradeReconMonitor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OverWatcher.Common.HelperFunctions;
using System.Data;

namespace OverWatcher.TradeReconMonitor.Core.Tests
{
#if TEST
    [TestClass()]
    public class ICEOpenLinkComparatorTests
    {
        [TestMethod()]
        public void DiffValidation()
        {
            ICEOpenLinkComparator comparator = new ICEOpenLinkComparator();
            var ice = HelperFunctions.CSVToDataTable("Testcase/CBNAFutures_ICE.csv");
            var opl = HelperFunctions.CSVToDataTable("Testcase/CBNAFutures_DB.csv");
            comparator.SwapLegIDAndDealID(ice);
            HelperFunctions.SortDataTable<int>(ice, "Deal ID", SortDirection.ASC);
            HelperFunctions.SortDataTable<int>(opl, "ICEDEALID", SortDirection.ASC);
            HelperFunctions.SaveDataTableToCSV(ice, "aaa");
            HelperFunctions.SaveDataTableToCSV(opl, "aaa");
            DataTable diff = comparator.Diff(ice, opl);
            HelperFunctions.SaveDataTableToCSV(diff, "sdsdsdsds");
            Assert.IsTrue(true);
        }

        [TestMethod()]
        public void DataTableTest()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("a");
            DataRow dr = dt.NewRow();
            dr["a"] = "aa";
            dt.Rows.Add(dr);
            DataTable cd = dt;
            dt.Rows.RemoveAt(0);
            Assert.IsTrue(object.ReferenceEquals(cd, dt));
        }
    }
#endif
}