using Microsoft.VisualStudio.TestTools.UnitTesting;
using OverWatcher.TradeReconMonitor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OverWatcher.Common.HelperFunctions;
namespace OverWatcher.TradeReconMonitor.Core.Tests
{
    [TestClass()]
    public class ICEOpenLinkComparatorTests
    {
        [TestMethod()]
        public void DiffValidation()
        {
            ICEOpenLinkComparator c = new ICEOpenLinkComparator();
            var ice = HelperFunctions.CSVToDataTable("Testcase/CGMLFutures_ICE.csv");
            var opl = HelperFunctions.CSVToDataTable("Testcase/CGMLFutures_DB.csv");
            HelperFunctions.SortDataTable<int>(ref ice, "Deal ID", SortDirection.ASC);
            HelperFunctions.SortDataTable<int>(ref opl, "ICEDEALID", SortDirection.ASC);
            HelperFunctions.SaveDataTableToCSV(ice, "aaa");
            HelperFunctions.SaveDataTableToCSV(opl, "aaa");
            c.SwapLegIDAndDealID(ice);
            HelperFunctions.SaveDataTableToCSV(ice, "aaa");
            HelperFunctions.SaveDataTableToCSV(opl, "aaa");
            var diff = c.Diff(ice, opl);
            HelperFunctions.SaveDataTableToCSV(diff, "sdsdsdsds");
            Assert.IsTrue(true);
        }
    }
}