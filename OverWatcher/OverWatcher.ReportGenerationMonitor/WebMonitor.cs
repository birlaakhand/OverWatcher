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

namespace OverWatcher.ReportGenerationMonitor
{
    class WebMonitor : WebControllerBase
    {
        private const string URL = "https://www.theice.com/marketdata/reports/10";
        private static DateTime NextReport = DateTimeHelper.ZoneNow;
        private static DateTime CurrentReport = DateTimeHelper.ZoneNow;
        private static string FormatDate(DateTime now)
        {
            return now.ToString("MMMM d, yyyy", new CultureInfo("en-US"));
        }

        private static DateTime StringToDate(string date)
        {
            DateTime d = default(DateTime);
            DateTime.TryParseExact(date, "MMMM d, yyyy", null, DateTimeStyles.AssumeLocal, out d);
            return d;
        }
        public WebMonitor() : base(ConfigurationManager.AppSettings["TempFolderPath"])
        {
            BrowserList.Add(AnalyzeWebsite);
        }
        private void AnalyzeWebsite()
        {
            try
            {
                Logger.Info("Loading Page");
                var browser = new ChromiumWebBrowser(URL);
                browser.DownloadHandler = new DownloadHandler(this);
                browser.LoadingStateChanged += AnalyzePage;
                _pageAnalyzeFinished.WaitOne();
                browser.Dispose();
                System.Windows.Forms.Application.ExitThread();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WebMonitor Failed");
                isError = true;
            }
        }
        private async void AnalyzePage(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;
            if (await IsPageLoading(wb, e)) return;
            Logger.Info("Webpage Loaded, Start Analyzing");
            await SavePageScreenShot(wb, "");
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

            scriptTask = await EvaluateXPathScriptAsync(wb, "//div/a/span", ".textContent =\"B-Brent Crude Future\"");

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
                string time = NextReport.ToString("yyyy-MM-dd_hh:mm:ss");
                string path = ConfigurationManager.AppSettings["TempFolderPath"] + string.Format("WebpageScreenshot_{0}.png", time);
                Thread.Sleep(2000); //allow page to render, JS rendering cannot detected by code
                Logger.Info("Report Found, Generation Time approx to " + time);
                Logger.Info("Saving Screenshot...");
                await SavePageScreenShot(wb, path);
                if (Environment.UserInteractive)
                {
                    using (EmailNotifier email = new EmailNotifier())
                    {
                        email.SendResultEmail("B-Brent Crude Future Report Generated At " + time, "", new List<string> { path });
                    }
                }
            }
            else
            {
                Logger.Info("The Report is not Ready Yet");
            }
            _pageAnalyzeFinished.Set();
        }
    }
}
