using Microsoft.VisualStudio.TestTools.UnitTesting;
using OverWatcher.TradeReconMonitor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OverWatcher.Common.HelperFunctions;
using System.Data;
using OverWatcher.Common;

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
            HelperFunctions.SortDataTable<int>(ref ice, "Deal ID", SortDirection.ASC);
            HelperFunctions.SortDataTable<int>(ref opl, "ICEDEALID", SortDirection.ASC);
            HelperFunctions.SaveDataTableToCSV(ice, "aaa");
            HelperFunctions.SaveDataTableToCSV(opl, "aaa");
            DataTable diff = comparator.Diff(ice, opl);
            HelperFunctions.SaveDataTableToCSV(diff, "sdsdsdsds");
            Assert.IsTrue(true);
        }
        [TestMethod()]
        public void SchedulerTest()
        {
            DateTime dt;
            DateTime.TryParse("2017-01-16 23:55:00",  out dt);
            dt = dt.AddMilliseconds(15 * 60 * 1000);
            int i = 0;
        }
    
}
#endif
}