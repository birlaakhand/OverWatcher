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
        private DateTime ReportToBeFound = DateTimeHelper.ZoneNow.AddDays(-1);
        private DateTime CurrentReport = DateTimeHelper.ZoneNow;
        public string ReportName;
        public string ResultTime
        {
            get;
            private set;
        }
        public string AttachmentPath
        {
            get;
            private set;
        }
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
            string resultToBeFound = FormatDate(ReportToBeFound);
            scriptTask = await EvaluateXPathScriptAsync(wb, string.Format("//div/div/div/table/tbody/tr/td[contains(., '{0}')]", resultToBeFound), ".innerHTML");
            if (scriptTask.Result != null && scriptTask.Result.ToString().Contains(resultToBeFound))
            {
                ResultTime = FormatDate(DateTimeHelper.ZoneNow);
                AttachmentPath = System.IO.Path.GetFullPath(ConfigurationManager.AppSettings["TempFolderPath"]) + string.Format("WebpageScreenshot_{0}_{1}.png", ResultTime, ReportName);
                Thread.Sleep(2000); //allow page to render, JS rendering cannot detected by code
                Logger.Info(ReportName + " Report Found, Generation Time approx to " + ResultTime);
                Logger.Info("Saving Screenshot...");
                await SavePageScreenShot(wb, AttachmentPath);
                CurrentReport = ReportToBeFound;
                ReportToBeFound = ReportToBeFound.AddDays(1);
            }
            else
            {
                Logger.Info(ReportName + " Report is not Ready Yet");
            }
            _pageAnalyzeFinished.Set();
        }
    }
}
