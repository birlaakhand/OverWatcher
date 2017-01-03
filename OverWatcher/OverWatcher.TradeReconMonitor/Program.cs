using CefSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using OverWatcher.Common.HelperFunctions;
using OverWatcher.Common.Scheduler;
using System.Threading;

namespace OverWatcher.TradeReconMonitor.Core
{
    class Program
    {
        private static bool EnableComparison;
        private static bool EnableSaveLocal;
        private static bool EnableEmail;
        private static string projectPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void Main(string[] args)
        {
            Start(args);
            Stop();
        }


        public static void Start(string[] args)
        {
            WebTradeMonitor.InitializeEnvironment();
            var schedule = new Schedule(ConfigurationManager.AppSettings["Frequency"],
                                            ConfigurationManager.AppSettings["FrequencyValue"],
                                            ConfigurationManager.AppSettings["Skip"],
                                            ConfigurationManager.AppSettings["SkipValue"]);
            if (!schedule.isSingleRun())
            {
                int interval = 1;
                int.TryParse(ConfigurationManager.AppSettings["SchedulerBaseInterval"], out interval);
                TaskScheduler scheduler = new TaskScheduler(interval * 1000);
                scheduler.AddTask(() =>
                {
                    StartReconsiliation();
                }, schedule);
                scheduler.Start();
                Console.WriteLine("Press \'q\' to quit the sample.");
                while (Console.Read() != 'q') ;
            }
            else
            {
                log.Info("Single Run of Task");
                StartReconsiliation();
                log.Info("Checking Finished");
            }

        }

        public static void Stop()
        {
            WebTradeMonitor.CleanupEnvironment();
        }
        private static void LoadOptions()
        {
            EnableComparison = ConfigurationManager.AppSettings["EnableComparison"] == "true" ? true : false;
            EnableEmail = ConfigurationManager.AppSettings["EnableEmail"] == "true" ? true : false;
            EnableSaveLocal = ConfigurationManager.AppSettings["EnableSaveLocal"] == "true" ? true : false;
        }
        private static void StartReconsiliation()
        {
            if (!Directory.Exists(ConfigurationManager.AppSettings["TempFolderPath"]))
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["TempFolderPath"]);
            }
            if (!Directory.Exists(ConfigurationManager.AppSettings["OutputFolderPath"]))
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["OutputFolderPath"]);
            }
            if (!Directory.Exists(ConfigurationManager.AppSettings["PersistentFolderPath"]))
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PersistentFolderPath"]);
            }
            log.Info(string.Format("Start Reconsiliate Trade at {0}",
                    DateTime.Now.ToString("MM/dd/yyyy hh:mm")));
            LoadOptions();
            WebTradeMonitor p = new WebTradeMonitor();
            log.Info("Clean up Temp Folder...");
            CleanUpTempFolder();
            p.run();
            p.LogCount();
            using (ExcelParser parser = new ExcelParser())
            {
                if (!EnableComparison)
                {
                    log.Info("Non Comparison Mode");
                    log.Info("Saving To Local...");
                    parser.SaveAsCSV();
                    if (!EnableEmail)
                    {
                        log.Info("Saving Count Result...");
                        p.OutputCountToFile();
                    }
                    else
                    {
                        log.Info("Add Count Result To Email...");
                        using (EmailHandler email = new EmailHandler())
                        {
                            email.SendResultEmail(p.CountToHTML(), null);
                        }
                    }
                }
                else
                {
                    log.Info("Start Comparison...");
                    try
                    {
                        var ICEResult = parser.GetDataTableList();
                        OracleDBMonitor db = new OracleDBMonitor();
                        int queryDelay = 0;
                        int.TryParse(ConfigurationManager.AppSettings["DBQueryDelay"], out queryDelay);
                        log.Info(string.Format("Wait to query Database, waiting time = {0} seconds", queryDelay));
                        Thread.Sleep(queryDelay * 1000);
                        var DBResult = db.QueryDB();
                        db.LogCount();
                        var diff = new ICEOpenLinkComparator().Diff(ICEResult, DBResult);
                        diff.ForEach(d => ExcelParser.DataTableCorrectDate(ref d, "Trade Date"));
                        if (EnableEmail)
                        {
                            //to-do
                            log.Info("Email Enabled...");
                            using (EmailHandler email = new EmailHandler())
                            {
                                log.Info("Add Count Result To Email...");
                                log.Info("Add Comparison Result To Email...");
                                var attachmentPaths = diff.Select(d => projectPath + HelperFunctions.SaveDataTableToCSV(d, "_Diff")).ToList();
                                log.Info("Add Comparison Result To Attachment...");
                                email.SendResultEmail(p.CountToHTML() + db.CountToHTML() + Environment.NewLine + BuildComparisonResultBody(diff), attachmentPaths);
                            }
                        }
                        if (EnableSaveLocal)
                        {
                            log.Info("Saving To Local...");
                            p.OutputCountToFile();
                            DBResult.ForEach(d => HelperFunctions.SaveDataTableToCSV(d, "_DB"));
                            ICEResult.ForEach(d => HelperFunctions.SaveDataTableToCSV(d, "_ICE"));
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Comparison Failed...   " + ex);
                    }

                }
            }
        }

        private static string BuildComparisonResultBody(List<DataTable> diff)
        {
            return string.Join(Environment.NewLine + Environment.NewLine, diff.Select(d => HTMLGenerator.DataTableToHTML(d)));
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
