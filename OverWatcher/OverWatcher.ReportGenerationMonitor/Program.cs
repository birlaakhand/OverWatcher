using OverWatcher.Common;
using OverWatcher.Common.CefSharpBase;
using OverWatcher.Common.HelperFunctions;
using OverWatcher.Common.Logging;
using OverWatcher.Common.Scheduler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using OverWatcher.Common.DateTimeHelper;

namespace OverWatcher.ReportGenerationMonitor
{
    public static class Program
    {
        public const string ServiceName = "ReportGenerationMonitoringService";
        private static readonly ConcurrentDictionary<string, DateTime> ReportMap;
        static Program()
        {
            ReportMap = new ConcurrentDictionary<string, DateTime>();
            double TMinus = -1;
            double.TryParse(ConfigurationManager.AppSettings["ReportMonitoringBaseDate"].ToString(), out TMinus);
            DateTime baseTime = DateTimeHelper
                                .ZoneNow
                                .AddWorkingDays(TMinus);
            if(!ConfigurationManager
                .AppSettings["ReportList"]
                .ToString().Split(";".ToCharArray())
                .ToList()
                .Select(rp => ReportMap.TryAdd(rp, baseTime))
                .Aggregate((b1, b2) => b1 & b2))
            {
                const string error = "Illegal Report Name";
                throw new Exception(error);
            }
        }

        public static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                // running as service
                using (var service = new Service())
                    ServiceBase.Run(service);
            }
            else
            {
                Console.WriteLine("Press \'q\' to quit.");
                Console.Title = "OverWatcher.ReportGenerationMonitor - Enter 'q' to Quit The Program";
                Start(args);
                Stop();
            }

        }

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

        public static void Start(string[] args)
        {
            if (!System.IO.Directory.Exists(ConfigurationManager.AppSettings["TempFolderPath"]))
            {
                System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["TempFolderPath"]);
            }
            BrowserWatcherBase.InitializeEnvironment();
            var schedule = new Schedule(ConfigurationManager.AppSettings["Frequency"],
                                            ConfigurationManager.AppSettings["FrequencyValue"],
                                            ConfigurationManager.AppSettings["Skip"],
                                            ConfigurationManager.AppSettings["SkipValue"]);
            if (!schedule.IsSingleRun())
            {
                Logger.Info("Start in Scheduled Mode");
                int interval = 1;
                int.TryParse(ConfigurationManager.AppSettings["SchedulerBaseInterval"], out interval);
                Common.Scheduler.TaskScheduler scheduler = new Common.Scheduler.TaskScheduler(interval);
                scheduler.AddTask(StartWebController, schedule);
                scheduler.Start();
                while (Console.Read() != 'q') ;
            }
            else
            {
                Logger.Info("Start in Single Run Mode");
                StartWebController();
                Logger.Info("Checking Finished");
            }

        }
        public static void Stop()
        {
            BrowserWatcherBase.CleanupEnvironment();
        }

        public static void StartWebController()
        {
            var reports = ReportMap
                            .Select(rl => new ReportMonitor(rl.Key, rl.Value))
                            .ToList();

            var isAsync = ConfigurationManager.AppSettings["RunAsync"]??"false";
            bool flag = false;
            if(isAsync == "true")
            {
                var threads = reports.Select(rp => rp.RunAsync());
                foreach(var td in threads)
                {
                    td.Wait();
                }
            }
            else
            {
                foreach(var report in reports)
                {
                    report.Run();
                }
            }
            flag = reports.Select(rp => rp.IsFound)
                            .Aggregate((b1, b2) => b1 | b2);
            if (!flag)
            {
                Logger.Info("No new Report Found, Skip the Email Notification");
                return;
            }
            foreach(var pair in ReportMap)
            {
                ReportMap[pair.Key] = ReportMap[pair.Key].AddWorkingDays(1);
            }

            if (Environment.UserInteractive)
            {
                using (EmailNotifier email = new EmailNotifier())
                {
                    var unfound = string.Join(Environment.NewLine
                                , reports.Where(rp => !rp.IsFound)
                                .Select(rp =>
                                rp.ReportName
                                + "For "
                                + ReportMonitor
                                .FormatDate(rp.ReportToBeFound) + " Not Found"));
                            

                    var found = string.Join(Environment.NewLine
                                , reports.Where(rp => rp.IsFound)
                                .Select(rp => rp.ReportName
                                        + "For "
                                        + ReportMonitor
                                        .FormatDate(rp.ReportToBeFound) + " Report Generated At " + rp.ResultTime));
                    email.SendResultEmail(
                            HelperFunctions.WrapParagraphToHTML(unfound
                            + Environment.NewLine
                            + found), ""
                                    , reports.Select(rp => rp.AttachmentPath).ToList());
                }
            }
        }
    }
}
