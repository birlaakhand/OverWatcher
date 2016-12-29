﻿using CefSharp;
using OverWatcher.TheICETrade;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
namespace OverWatcher
{

    class Program
    {
        private static bool EnableComparison;
        private static bool EnableSaveLocal;
        private static bool EnableEmail;
        private static string projectPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #region Service Runner
        public const string ServiceName = "TradeReconMonitor";
        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = Program.ServiceName;
            }
            protected override void OnStart(string[] args)
            {
                Program.Start(args);
            }
            protected override void OnStop()
            {
                Program.Stop();
            }
        }
        #endregion
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += DisposableCleaner.OnClose;
            Schedule();
            Terminate();
        }


        private static void Start(string[] args)
        {
            Schedule();
        }

        private static void Stop()
        {
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

                log.Info(string.Format("Run Checking at {0}",
                        DateTime.Now.ToString("MM/dd/yyyy hh:mm")));
                LoadOptions();
                WebTradeMonitor p = new WebTradeMonitor();
                log.Info("Clean up old Excel...");
                CleanUpTempFolder();
                p.run();
                p.LogCount();
                using (ExcelParser parser = new ExcelParser())
                {
                    DisposableCleaner.ManageDisposable(parser);
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
                                DisposableCleaner.ManageDisposable(email);
                                email.SendResultEmail(p.CountToHTML(), null);
                            }
                        }
                    }
                    else
                    {
                        log.Info("Start Comparison...");
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
                                log.Info("Email Enabled...");
                                using (EmailHandler email = new EmailHandler())
                                {
                                    DisposableCleaner.ManageDisposable(email);
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
                if (interval < 1) return;
                log.Info(string.Format(
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
