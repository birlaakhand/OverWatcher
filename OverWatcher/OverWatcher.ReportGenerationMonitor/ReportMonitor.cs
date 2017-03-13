﻿using CefSharp;
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
        private DateTime ReportToBeFound = DateTimeHelper.ZoneNow.AddWorkingDays(-1);
        private DateTime CurrentReport = DateTime.MinValue;
        public string ReportName;

        public bool IsFound
        {
            get
            {
                return CurrentReport == DateTimeHelper.ZoneNow.AddWorkingDays(-1);
            }
        }
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

        private static string FormatDateWithTime(DateTime now)
        {
            return now.ToString("yyyy-MM-dd-hh:mm:ss", new CultureInfo("en-US"));
        }
        public ReportMonitor(string ReportName) : base(ConfigurationManager.AppSettings["TempFolderPath"])
        {
            this.ReportName = ReportName;
        }
        protected override Task StartBrowser()
        {
            try
            {
                Logger.Info(ReportName + " Loading Page");
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
            Logger.Info(ReportName + " Webpage Loaded, Start Analyzing, Finding Report:" + ReportName);
            JavascriptResponse scriptTask = await EvaluateXPathScriptAsync(wb, "//button[contains(., 'I Accept')]", ".innerHTML");    
                
            if (scriptTask.Result != null && scriptTask.Result.ToString() == "I Accept")
            {
                await EvaluateXPathScriptAsync(wb, "//button[contains(., 'I Accept')]", ".click()");
            }
            Logger.Info(ReportName + " Loading Login Page");
            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//button[contains(., 'I Accept')]", ".innerHTML");
            }
            while (scriptTask.Result != null && scriptTask.Result.ToString() == "I Accept");
            Logger.Info(ReportName + " Loading Report Options");
            do
            {
                scriptTask = await wb.EvaluateScriptAsync("document.getElementById(\"report-content\").className");
            }
            while (scriptTask.Result.ToString().Contains("is-loading"));

            Logger.Info("Cleaning Up Selection");
            do
            {
                await EvaluateXPathScriptAsync(wb, "//div/select/option[@selected='selected']", ".removeAttribute('selected')");
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/select/option[@selected='selected']", "");
            }
            while (scriptTask.Result != null);
            Logger.Info(ReportName + " Selecting Report");
            do
            {
                await EvaluateXPathScriptAsync(wb, "//div/select/option[contains(.,'" + ReportName + "')]", ".setAttribute('selected', 'selected')");
                Thread.Sleep(100);
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/select/option[@selected='selected']", ".innerHTML");
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() != ReportName);

            await EvaluateXPathScriptAsync(wb, "//form/input[@value='Submit']", ".click()");
            Logger.Info(ReportName + " Loading Report Content");
            do
            {
                scriptTask = await wb.EvaluateScriptAsync("document.getElementById(\"report-content\").className");
                Thread.Sleep(100);
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString().Contains("is-loading"));
            Logger.Info(ReportName + " Loading Report Content Table");
            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/div/div/table", ".className");
                Thread.Sleep(100);
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() != "table table-data table-responsive");
            Logger.Info(ReportName + " Loading Report Content Table Rows");
            do
            {
                scriptTask = await EvaluateXPathScriptAsync(wb, "//div/div/div/table/tbody/tr/td", ".innerHTML");
                Thread.Sleep(100);
            }
            while (scriptTask.Result == null || scriptTask.Result.ToString() == string.Empty);
            await EvaluateXPathScriptAsync(wb, "//div/div/div/table/tbody/tr", ".innerHTML");
            string resultToBeFound = FormatDate(ReportToBeFound);
            scriptTask = await EvaluateXPathScriptAsync(wb, string.Format("//div/div/div/table/tbody/tr/td[contains(., '{0}')]", resultToBeFound), ".innerHTML");
            if (scriptTask.Result != null && scriptTask.Result.ToString().Contains(resultToBeFound))
            {
                ResultTime = FormatDateWithTime(DateTimeHelper.ZoneNow);
                AttachmentPath = System.IO.Path.GetFullPath(ConfigurationManager.AppSettings["TempFolderPath"]) + string.Format("WebpageScreenshot_{0}_{1}.png", ResultTime.Replace(':', '-'), ReportName);
                Thread.Sleep(2000); //allow page to render, JS rendering cannot detected by code
                Logger.Info(ReportName + " Report Found, Generation Time approx to " + ResultTime);
                Logger.Info(ReportName + " Saving Screenshot...");
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
