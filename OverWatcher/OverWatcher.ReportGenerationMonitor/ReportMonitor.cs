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
using OverWatcher.Common.DateTimeHelper;

namespace OverWatcher.ReportGenerationMonitor
{
    class ReportMonitor : BrowserWatcherBase
    {
        public string Url = "https://www.theice.com/marketdata/reports/10";
        public DateTime ReportToBeFound { get; private set; }
        public string ReportName;

        public bool IsFound { get; private set; }
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
        public static string FormatDate(DateTime now)
        {
            return now.ToString("MMMM d, yyyy", new CultureInfo("en-US"));
        }

        private static string FormatDateWithTime(DateTime now)
        {
            return now.ToString("yyyy-MM-dd-hh:mm:ss", new CultureInfo("en-US"));
        }
        public ReportMonitor(string reportName, DateTime reportToBeFound) : base(ConfigurationManager.AppSettings["TempFolderPath"])
        {
            this.ReportName = reportName;
            ReportToBeFound = reportToBeFound;
        }
        protected override Task StartBrowser()
        {
            try
            {
                Logger.Info(ReportName + " Loading Page");
                var browser = new ChromiumWebBrowser(Url);
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
            Logger.Info(ReportName + " Webpage Loaded, Start Analyzing, Finding Report:" + ReportName);
            JavascriptResponse scriptTask = await wb.EvaluateXPathScriptAsync("//button[contains(., 'I Accept')]", ".innerHTML");    
                
            if (scriptTask.Result != null && scriptTask.Result.ToString() == "I Accept")
            {
                await wb.EvaluateXPathScriptAsync("//button[contains(., 'I Accept')]", ".click()");
            }
            Logger.Info("Loading Login Page");
            do
            {
                scriptTask = await wb.EvaluateXPathScriptAsync("//button[contains(., 'I Accept')]", ".innerHTML");
            }
            while (scriptTask.Result != null && scriptTask.Result.ToString() == "I Accept");
            Logger.Info("Loading Report Options");
            do
            {
                scriptTask = await wb.EvaluateScriptAsync("document.getElementById(\"report-content\").className");
            }
            while (scriptTask.Result.ToString().Contains("is-loading"));

            Logger.Info("Cleaning Up Selection");
            do
            {
                await wb.EvaluateXPathScriptAsync("//div/select/option[@selected='selected']", ".removeAttribute('selected')");
                scriptTask = await wb.EvaluateXPathScriptAsync("//div/select/option[@selected='selected']", "");
            }
            while (scriptTask.Result != null);
            Logger.Info("Selecting Report");
            do
            {
                await wb.EvaluateXPathScriptAsync("//div/select/option[contains(.,'" + ReportName + "')]", ".setAttribute('selected', 'selected')");
                Thread.Sleep(100);
                scriptTask = await wb.EvaluateXPathScriptAsync("//div/select/option[@selected='selected']", ".innerHTML");
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() != ReportName);

            await wb.EvaluateXPathScriptAsync("//form/input[@value='Submit']", ".click()");
            Logger.Info("Loading Report Content");
            do
            {
                scriptTask = await wb.EvaluateScriptAsync("document.getElementById(\"report-content\").className");
                Thread.Sleep(100);
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString().Contains("is-loading"));
            Logger.Info("Loading Report Content Table");
            do
            {
                scriptTask = await wb.EvaluateXPathScriptAsync("//div/div/div/table", ".className");
                Thread.Sleep(100);
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() != "table table-data table-responsive");
            Logger.Info("Loading Report Content Table Rows");
            do
            {
                scriptTask = await wb.EvaluateXPathScriptAsync("//div/div/div/table/tbody/tr/td", ".innerHTML");
                Thread.Sleep(100);
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() == string.Empty);
            await wb.EvaluateXPathScriptAsync("//div/div/div/table/tbody/tr", ".innerHTML");
            string resultToBeFound = FormatDate(ReportToBeFound);
            scriptTask = await wb.EvaluateXPathScriptAsync(string.Format("//div/div/div/table/tbody/tr/td[contains(., '{0}')]", resultToBeFound), ".innerHTML");
            if (scriptTask.Result != null && scriptTask.Result.ToString().Contains(resultToBeFound))
            {
                ResultTime = FormatDateWithTime(DateTimeHelper.ZoneNow);
                AttachmentPath = System.IO.Path.GetFullPath(ConfigurationManager.AppSettings["TempFolderPath"]) + string.Format("WebpageScreenshot_{0}_{1}.png", ResultTime.Replace(':', '-'), ReportName);
                Thread.Sleep(2000); //allow page to render, JS rendering cannot detected by code
                Logger.Info(ReportName + " Report Found, Generation Time approx to " + ResultTime);
                IsFound = true;
                Logger.Info(ReportName + " Saving Screenshot...");
                await wb.SavePageScreenShot(AttachmentPath);
            }
            else
            {
                Logger.Info(ReportName + " Report is not Ready Yet");
                IsFound = false;
            }
            _pageAnalyzeFinished.Set();
        }
    }
}
