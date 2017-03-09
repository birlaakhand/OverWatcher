using OverWatcher.Common.CefSharpBase;
using OverWatcher.Common.Logging;
using OverWatcher.Common.Scheduler;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;

namespace OverWatcher.ReportGenerationMonitor
{
    public static class Program
    {
        public const string ServiceName = "ReportGenerationMonitoringService";
        private static readonly string[] ReportList = ConfigurationManager
                                                .AppSettings["ReportList"]
                                                .ToString().Split(";".ToCharArray());
        static void Main(string[] args)
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
            WebControllerBase.InitializeEnvironment();
            var schedule = new Schedule(ConfigurationManager.AppSettings["Frequency"],
                                            ConfigurationManager.AppSettings["FrequencyValue"],
                                            ConfigurationManager.AppSettings["Skip"],
                                            ConfigurationManager.AppSettings["SkipValue"]);
            if (!schedule.isSingleRun())
            {
                Logger.Info("Start in Scheduled Mode");
                int interval = 1;
                int.TryParse(ConfigurationManager.AppSettings["SchedulerBaseInterval"], out interval);
                Common.Scheduler.TaskScheduler scheduler = new Common.Scheduler.TaskScheduler(interval);
                scheduler.AddTask(() =>
                {
                    StartWebController();
                }, schedule);
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
            WebControllerBase.CleanupEnvironment();
        }

        public static void StartWebController()
        {
            var reports = ReportList.Select(rl => new ReportMonitor(rl));
            foreach(var report in reports)
            {
                report.Run();
            }
            if (Environment.UserInteractive)
            {
                using (EmailNotifier email = new EmailNotifier())
                {
                    
                    email.SendResultEmail(
                        reports.Where(rp => !rp.IsFound).Select(rp => rp.ReportName + " Not Found")
                            .Aggregate((a, b) => a + Environment.NewLine + b)
                            + Environment.NewLine + 
                        reports.Where(rp => rp.IsFound).Select(rp => rp.ReportName + " Report Generated At " + rp.ResultTime)
                            .Aggregate((a, b) => a + Environment.NewLine + b)
                            , ""
                            , reports.Select(rp => rp.AttachmentPath).ToList());
                }
            }
        }
    }
}
