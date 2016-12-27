using OverWatcher.TheICETrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher
{
    class Program
    {
        public static void Main(string[] args)
        {
            DealsReportMonitor.Schedule();
            DealsReportMonitor.Terminate();
        }

        public static void Main2(string[] args)
        {
            new DealsReportMonitor().QueryDB();
        }
    }
}
