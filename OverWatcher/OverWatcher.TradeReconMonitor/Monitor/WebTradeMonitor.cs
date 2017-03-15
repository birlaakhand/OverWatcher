using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using CefSharp.OffScreen;
using CefSharp;
using System.Configuration;
using System.Threading.Tasks;
using System.Text;
using System.Data;
using OverWatcher.Common.Logging;
using OverWatcher.Common;
using OverWatcher.Common.Interface;
using OverWatcher.Common.CefSharpBase;
using OverWatcher.Common.DateTimeHelper;

namespace OverWatcher.TradeReconMonitor.Core
{
    public enum ProductType { Swap, Futures };
    public enum CompanyName { CBNA, CGML };
    class WebTradeMonitor : BrowserWatcherBase,ITradeMonitor
    {        
        private static readonly string Url = ConfigurationManager.AppSettings["TargetUrl"];
        private static readonly Dictionary<string, string> NameMap = new Dictionary<string, string>
        {
            { "Citibank, N.A", "CBNA" },
            { "Citigroup Global Markets Ltd Global Commodities", "CGML"}
        };

        public WebTradeMonitor() : base(ConfigurationManager.AppSettings["TempFolderPath"]) { }

        public int Futures { get; private set; }

        public int Swap { get; private set; }

        public string MonitorTitle => "ICETrade";

        #region Login

        private void BuildPostLoad(out string post, string otp)
        {
            post =
               "{\"userId\":\"" + ConfigurationManager.AppSettings["Username"]
                        + "\",\"password\":\"" + ConfigurationManager.AppSettings["Password"]
                        + "\",\"appKey\":\"reports\",\"otpCode\":\" " + (otp ?? String.Empty) + "\"}";
        }


        private string Urlpostfix()
        {
            return "?_=" + Math.Floor(DateTime.Now.ToUniversalTime().Subtract(
                            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                ).TotalMilliseconds).ToString();
        }

        private CookieContainer RequestSSOCookie()
        {
            try
            {
                var cookies = ReadCookiesFromDisk(null);
                HttpWebResponse response;
                #region Validate Existing SSO Token
                if (null != cookies)
                {
                    var cookie = new CookieContainer();
                    cookie.SetCookies(new Uri(Url), cookies);
                    response = MakeHttpRequest(Url
                        + ConfigurationManager.AppSettings["PrincipalUrl"]
                        + Urlpostfix(), null, cookie);
                    if (GetResponseString(response).Contains("Valid Token"))
                    {
                        response.Close();
                        return cookie;
                    }
                    else
                    {
                        File.Delete(_defaultCookiePath);
                        OutputTo(ConfigurationManager.AppSettings["OTPExpiredAlertMessage"],
                            ConfigurationManager.AppSettings["OTPExpiredAlertMessage"]);
                    }
                    response.Close();
                }
                #endregion
                #region Get SSO Token
                DateTime requestTime = DateTime.Now;
                string SSOUrl = ConfigurationManager.AppSettings["SSOUrl"];
                string post = "";
                BuildPostLoad(out post, null);
                byte[] encodedPost = System.Text.Encoding.UTF8.GetBytes(post);
                response = MakeHttpRequest(Url + SSOUrl + Urlpostfix(), encodedPost, null);
                if (GetResponseString(response).Contains("Re-login with 2FA passcode"))
                {
                    Logger.Info("OTP Expired, Get OTP from Outlook");
                    string otp = "";
                    using (EmailController email = new EmailController())
                    {
                        otp = email.GetOTP(requestTime).ToString();
                    }
                    if(otp == "")
                    {
                        Logger.Info("Please enter OTP:");
                        otp = Console.ReadLine();
                    }
                    else
                    {
                        Logger.Info("OTP successfully load from Outlook");
                    }
                    response.Close();
                    BuildPostLoad(out post, otp);
                    encodedPost = System.Text.Encoding.UTF8.GetBytes(post);
                    response = MakeHttpRequest(Url + SSOUrl + Urlpostfix(), encodedPost, null);
                }
                CookieContainer collection = new CookieContainer();
                string sso = GetCookieHeader(response);
                if (!sso.Contains("iceSsoCookie")) return RequestSSOCookie();
                WriteCookiesToDisk(null, sso);
                collection.SetCookies(new Uri(Url), sso);
                response.Close();
                #endregion
                Logger.Info("SSO Cookie is Ready");
                return collection;
            }
            catch (Exception ex)
            {
                throw new MonitorException("Request SSO Cookie Failed", ex);
            }

        }
        #endregion
        #region Page Analyzer
        protected override Task StartBrowser()
        {
            try
            {
                var cookieManager = Cef.GetGlobalCookieManager();
                // Create the offscreen Chromium browser.
                var cookies = RequestSSOCookie().GetCookies(new Uri(Url));
                foreach (System.Net.Cookie cook in cookies)
                {
                    cookieManager.SetCookieAsync(Url + "reports/DealReport.shtml?",
                        ConvertCookie(cook));

                }
                var browser = new ChromiumWebBrowser(Url + "reports/DealReport.shtml?")
                {
                    DownloadHandler = new DownloadHandler(this)
                };
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
            Logger.Info("Analyzing Reports");
            DateTime now = DateTimeHelper.ZoneNow;
            var scriptTask = await wb.EvaluateScriptAsync(
                "document.getElementById('tradeBeginDate').value = '"
                 + now.ToString("dd-MMM-yyyy") + "'");
            scriptTask = await wb.EvaluateScriptAsync(
                    "document.getElementById('tradeEndDate').value = '"
                     + now.ToString("dd-MMM-yyyy") + "'");
            for (int i = 0; i < 2; ++i)
            {
                int temp = i;
                scriptTask = null;
                scriptTask = await wb.EvaluateScriptAsync(
                    $"document.getElementById('companyId').options[{temp}].selected = true;");
                while ((await wb.EvaluateScriptAsync(
                           $"console.log(document.getElementById('companyId').options[{temp}]);")).Message == "false") ;
                scriptTask = null;
                scriptTask = await wb.EvaluateScriptAsync(
                            "document.evaluate(\"//a[contains(., 'Show')]\", document, null, XPathResult.ANY_TYPE, null ).iterateNext().click();");
                JavascriptResponse waitingTask = null;
                Logger.Info("Retrieving Count - " + i);
                while (string.IsNullOrEmpty((waitingTask?.Result as string)))
                {

                    waitingTask = await wb.EvaluateScriptAsync(
                                "document.getElementById('futuresCount').innerHTML;");
                    Thread.Sleep(100);
                }
                scriptTask = waitingTask;
                string f = (scriptTask.Result as string);
                Futures += int.Parse(f.Substring(1, f.Length - 2));
                scriptTask = null;
                while (string.IsNullOrEmpty((scriptTask?.Result as string)))
                {
                    scriptTask = await wb.EvaluateScriptAsync(
                            "document.getElementById('clearedCount').innerHTML;");
                    Thread.Sleep(100);
                }
                f = ((string) scriptTask.Result);
                Swap += int.Parse(f.Substring(1, f.Length - 2));
                if(ConfigurationManager.AppSettings["EnableSaveWebpageScreenShot"] == "true")
                {
                    await wb.SavePageScreenShot(ConfigurationManager.AppSettings["TempFolderPath"] + "WebPageScreenShot.png");
                }
                wb.LoadingStateChanged -= AnalyzePage;
                await wb.EvaluateScriptAsync(
                    "document.getElementsByClassName('js-download-deals')[1].click();");
                while (!isDownloadCompleted) ;
                scriptTask = await wb.EvaluateScriptAsync(
                    $"document.getElementById('companyId').options[{temp}].innerHTML");
                var name = NameMap.Keys.FirstOrDefault(key => scriptTask.Result.ToString().Contains(key));
                if (name != null)
                {
                    RenameExcel(name);
                }
                isDownloadCompleted = false;
                DownloadFileName = "";
            }          
            _pageAnalyzeFinished.Set();
        }

        private void RenameExcel(string name)
        {
            var excelFiles = Directory.GetFiles(TempFolderPath, "*"
                + ConfigurationManager.AppSettings["DownloadedFileType"])
                                     .Select(Path.GetFileName);
            foreach (var file in excelFiles)
            {
                if (file != DownloadFileName) continue;
                System.IO.File.Move(TempFolderPath + file,
                    TempFolderPath +
                    Path.GetFileNameWithoutExtension(file) + "_" + name
                     + Path.GetExtension(file));
            }

        }
        #endregion


        private static void OutputTo(string futures, string cleared)
        {
            string outputPath = ConfigurationManager.AppSettings["OutputPath"];
            //after your loop
            File.WriteAllText(outputPath, FormatCount(futures, cleared));

        }
        public void OutputCountToFile()
        {
            OutputTo(this.Futures.ToString(), this.Swap.ToString()
                                    .Split("[".ToCharArray()).FirstOrDefault(name => !string.IsNullOrEmpty(name)));
        }
        private static string FormatCount(string futures, string cleared)
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("BOOK, TRADE_COUNT");
            csv.AppendLine($"cleared swap,{cleared}");
            csv.AppendLine($"future,{futures}");
            return csv.ToString();
        }
        private string FormatCount()
        {
            return FormatCount(this.Futures.ToString(), this.Swap.ToString());
        }

        public string CountToHTML()
        {

            return HTMLGenerator.CountToHTML(MonitorTitle, Swap, Futures);
        }

        public void LogCount()
        {
            Logger.Info(MonitorTitle + " Future count:" + Futures
                                + "   "
                                + "Cleared count:" + Swap);
        }
    }

}
