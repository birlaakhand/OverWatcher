using CefSharp;
using CefSharp.OffScreen;
using OverWatcher.Common;
using OverWatcher.Common.CefSharpBase;
using OverWatcher.Common.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OverWatcher.ReportGenerationMonitor
{
    class WebMonitor : WebControllerBase
    {
        private const string URL = "https://www.theice.com/marketdata/reports/10";
        public WebMonitor() : base(ConfigurationManager.AppSettings["TempFolderPath"])
        {
        }
        private void AnalyzeWebsite()
        {
            try
            {
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
            if (e.IsLoading) return;
            var html = await wb.GetSourceAsync();
            if (html == "<html><head></head><body></body></html>") return;
            Logger.Info("Analyzing Reports");
            DateTime now = DateTimeHelper.ZoneNow();
            var scriptTask = await wb.EvaluateScriptAsync(
                "document.getElementById('tradeBeginDate').value = '"
                 + now.ToString("dd-MMM-yyyy") + "'");
            scriptTask = await wb.EvaluateScriptAsync(
                    "document.getElementById('tradeEndDate').value = '"
                     + now.ToString("dd-MMM-yyyy") + "'");
            _pageAnalyzeFinished.Set();
        }
    }
}
