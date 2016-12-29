using CefSharp;
using OverWatcher.TheICETrade;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OverWatcher
{
    class Program
    {
        private static bool EnableComparison;
        private static bool EnableSaveLocal;
        private static bool EnableEmail;
        private static string projectPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        private static void Main(string[] args)
        {
            Schedule();
            Terminate();
        }

        private static void LoadOptions()
        {
            EnableComparison = ConfigurationManager.AppSettings["EnableComparison"] == "true" ? true : false;
            EnableEmail = ConfigurationManager.AppSettings["EnableEmail"] == "true" ? true : false;
            EnableSaveLocal = ConfigurationManager.AppSettings["EnableSaveLocal"] == "true" ? true : false;
        }

        private static void Schedule()
        {
            if (!Directory.Exists(ConfigurationManager.AppSettings["TempFolderPath"]))
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["TempFolderPath"]);
            }
            if (!Directory.Exists(ConfigurationManager.AppSettings["OutputFolderPath"]))
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["OutputFolderPath"]);
            }

            int interval = 0;
            int.TryParse(ConfigurationManager.AppSettings["ScheduleInterval"], out interval);
            while (true)
            {

                Console.WriteLine(string.Format("Run Checking at {0}",
                        DateTime.Now.ToString("MM/dd/yyyy hh:mm")));
                LoadOptions();
                WebTradeMonitor p = new WebTradeMonitor();
                Console.WriteLine("Clean up old Excel..");
                CleanUpTempFolder();
                p.run();
                p.LogCount();
                ExcelParser parser = new ExcelParser();
                if (!EnableComparison)
                {
                    Console.WriteLine("Non Comparison Mode");
                    Console.WriteLine("Saving To Local...");
                    parser.SaveAsCSV();
                    if (!EnableEmail)
                    {
                        Console.WriteLine("Saving Count Result..");
                        p.OutputCountToFile();
                    }
                    else
                    {
                        Console.WriteLine("Add Count Result To Email..");
                        using (EmailHandler email = new EmailHandler())
                        {
                            email.SendResultEmail(p.CountToHTML(), null);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Start Comparison..");
                    try
                    {
                        OracleDBMonitor db = new OracleDBMonitor();
                        var DBResult = db.QueryDB();
                        db.LogCount();
                        var ICEResult = parser.GetDataTableList();
                        var diff = new ICEOpenLinkComparator().Diff(ICEResult, DBResult);
                        diff.ForEach(d => ExcelParser.DataTableCorrectDate(ref d, "Trade Date"));
                        if (EnableEmail)
                        {
                            //to-do
                            Console.WriteLine("Email Enabled..");
                            using (EmailHandler email = new EmailHandler())
                            {
                                Console.WriteLine("Add Count Result To Email..");
                                Console.WriteLine("Add Comparison Result To Email..");
                                var attachmentPaths = diff.Select(d => projectPath + HelperFunctions.SaveDataTableToCSV(d, "_Diff")).ToList();
                                Console.WriteLine("Add Comparison Result To Attachment..");
                                email.SendResultEmail(p.CountToHTML() + db.CountToHTML() + Environment.NewLine + BuildComparisonResultBody(diff), attachmentPaths);
                            }
                        }
                        if (EnableSaveLocal)
                        {
                            Console.WriteLine("Saving To Local...");
                            p.OutputCountToFile();
                            DBResult.ForEach(d => HelperFunctions.SaveDataTableToCSV(d, "_DB"));
                            ICEResult.ForEach(d => HelperFunctions.SaveDataTableToCSV(d, "_ICE"));
                        }
                    }
                    catch (Exception ex)
                    {
                        parser.Dispose();
                        Console.WriteLine("Comparison Failed...");
                        Console.WriteLine(ex);
                    }

                }
                parser.Dispose();
                if (interval < 1) return;
                Console.WriteLine(string.Format(
                    "Checking Finished, Waiting for next run. Interval = {0} seconds",
                    int.Parse(ConfigurationManager.AppSettings["ScheduleInterval"])));
                Thread.Sleep(interval * 1000);
            }
        }

        private static string BuildComparisonResultBody(List<DataTable> diff)
        {
            return string.Join(Environment.NewLine + Environment.NewLine, diff.Select(d => HTMLGenerator.DataTableToHTML(d)));
        }
        private static void Terminate()
        {
            Cef.Shutdown();
        }
        private static void CleanUpTempFolder()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(ConfigurationManager.AppSettings["TempFolderPath"]);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
