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
            var ice = HelperFunctions.CSVToDataTable("");
            var opl = HelperFunctions.CSVToDataTable("");
            c.Diff(ice, opl);
            Assert.IsTrue(true);
        }
    }
}