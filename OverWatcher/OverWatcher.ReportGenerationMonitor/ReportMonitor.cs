using CefSharp;
using CefSharp.OffScreen;
using OverWatcher.Common;
using OverWatcher.Common.CefSharpBase;
using OverWatcher.Common.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace OverWatcher.ReportGenerationMonitor
{
    class ReportMonitor : WebControllerBase
    {
        public string URL = "https://www.theice.com/marketdata/reports/10";
        private DateTime NextReport = DateTimeHelper.ZoneNow.AddDays(-1);
        private DateTime CurrentReport = DateTimeHelper.ZoneNow;
        public string ReportName = "B-Brent Crude Future";
        private static string FormatDate(DateTime now)
        {
            return now.ToString("MMMM d, yyyy", new CultureInfo("en-US"));
        }
        public ReportMonitor(string ReportName) : base(ConfigurationManager.AppSettings["TempFolderPath"])
        {
            this.ReportName = ReportName;
        }
        protected override Task StartBrowser()
        {
            try
            {
                Logger.Info("Loading Page");
                var browser = new ChromiumWebBrowser(URL);
                browser.DownloadHandler = new DownloadHandler(this);
                browser.LoadingStateChanged += AnalyzePage;
                _pageAnalyzeFinished.WaitOne();
                browser.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WebMonitor Failed");
                throw;
            }
            return Task.FromResult<object>(null);
        }
        private async void AnalyzePage(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;
            if (await IsPageLoading(wb, e)) return;
            Logger.Info("Webpage Loaded, Start Analyzing");
            JavascriptResponse scriptTask = await EvaluateXPathScriptAsync(wb, "//button[contains(., 'I Accept')]", ".innerHTML");    
                
            if (scriptTask.Result != null && scriptTask.Result.ToString() == "I Accept")
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//button[contains(., 'I Accept')]", ".click()");
            }
            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//button[contains(., 'I Accept')]", ".innerHTML");
            }
            while (scriptTask.Result != null && scriptTask.Result.ToString() == "I Accept");

            do
            {
                scriptTask = await wb.EvaluateScriptAsync("document.getElementById(\"report-content\").className");
            }
            while (scriptTask.Result.ToString().Contains("is-loading"));

            scriptTask = await EvaluateXPathScriptAsync(wb, "//div/a/span", ".textContent =\"" + ReportName + "\"");

            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/a/span", ".innerHTML");
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() != "B-Brent Crude Future");

            scriptTask = await EvaluateXPathScriptAsync(wb, "//form/input[@value='Submit']", ".innerHTML");
            scriptTask = await EvaluateXPathScriptAsync(wb, "//form/input[@value='Submit']", ".click()");
            do
            {
                scriptTask = await wb.EvaluateScriptAsync("document.getElementById(\"report-content\").className");
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString().Contains("is-loading"));

            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/div/div/table", ".className");
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() != "table table-data table-responsive");

            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/div/div/table/tbody/tr/td", ".innerHTML");
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() == string.Empty);
            scriptTask = await EvaluateXPathScriptAsync(wb, "//div/div/div/table/tbody/tr", ".innerHTML");
            string today = FormatDate(NextReport);
            scriptTask = await EvaluateXPathScriptAsync(wb, string.Format("//div/div/div/table/tbody/tr/td[contains(., '{0}')]", today), ".innerHTML");
            if (scriptTask.Result != null && scriptTask.Result.ToString().Contains(today))
            {
                string time = NextReport.ToString("yyyy-MM-dd_hh-mm-ss");
                string path = System.IO.Path.GetFullPath(ConfigurationManager.AppSettings["TempFolderPath"]) + string.Format("WebpageScreenshot_{0}.png", time);
                Thread.Sleep(2000); //allow page to render, JS rendering cannot detected by code
                Logger.Info("Report Found, Generation Time approx to " + time);
                Logger.Info("Saving Screenshot...");
                await SavePageScreenShot(wb, path);
                if (Environment.UserInteractive)
                {
                    using (EmailNotifier email = new EmailNotifier())
                    {
                        email.SendResultEmail(ReportName + " Report Generated At " + time, "", new List<string> { path });
                    }
                }
                CurrentReport = NextReport;
                NextReport = NextReport.AddDays(1);
            }
            else
            {
                Logger.Info("The Report is not Ready Yet");
            }
            _pageAnalyzeFinished.Set();
        }
    }
}
