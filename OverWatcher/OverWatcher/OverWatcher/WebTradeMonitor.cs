﻿using System;
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
using System.Drawing;
using System.Data;

namespace OverWatcher.TheICETrade
{
    public enum ProductType { Swap, Futures };
    public enum CompanyName { CBNA, CGML };
    class WebTradeMonitor :TradeMonitorBase
    {
        private string _defaultCookiePath = ConfigurationManager.AppSettings["CookiePath"];
        private static string projectPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        private string _url = ConfigurationManager.AppSettings["TargetUrl"];
        private Dictionary<string, string> NameMap = new Dictionary<string, string>
        {
            { "Citibank, N.A", "CBNA" },
            { "Citigroup Global Markets Ltd Global Commodities", "CGML"}
        };

        public WebTradeMonitor() : base("ICETrade") { }
        #region Thread Share Fields
        internal volatile bool isDownloadCompleted = false;
        internal volatile string DownloadFileName = "";
        private AutoResetEvent _pageAnalyzeFinished = new AutoResetEvent(false);
        #endregion
        public void run()
        {
            var thread = new Thread(AnalyzeWebsite);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

        }
        #region Login
        private CefSharp.Cookie ConvertCookie(System.Net.Cookie cookie)
        {
            var c = new CefSharp.Cookie();
            c.Creation = cookie.TimeStamp;
            c.Domain = cookie.Domain;
            c.Expires = cookie.Expires;
            c.HttpOnly = cookie.HttpOnly;
            c.Name = cookie.Name;
            c.Path = cookie.Path;
            c.Secure = cookie.Secure;
            c.Value = cookie.Value;
            return c;
        }
        private void BuildPostLoad(out string post, string otp)
        {
            post =
               "{\"userId\":\"" + ConfigurationManager.AppSettings["Username"]
                        + "\",\"password\":\"" + ConfigurationManager.AppSettings["Password"]
                        + "\",\"appKey\":\"reports\",\"otpCode\":\" " + (otp ?? String.Empty) + "\"}";
        }

        private HttpWebResponse MakeHttpRequest(string url, byte[] encodedPost, CookieContainer container)
        {
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
            Stream dataStream;

            request.Method = "POST";
            request.ContentType = "application/json";
            if (encodedPost != null)
            {
                request.ContentLength = encodedPost.Length;
                dataStream = request.GetRequestStream();
                dataStream.Write(encodedPost, 0, encodedPost.Length);
                dataStream.Close();
            }

            if (container != null)
            {
                request.CookieContainer = container;
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            return response;
        }

        private string GetResponseString(HttpWebResponse response)
        {
            var dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseString = reader.ReadToEnd();
            dataStream.Close();
            response.Close();
            return responseString;
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
                    cookie.SetCookies(new Uri(_url), cookies);
                    response = MakeHttpRequest(_url
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
                        OutputTo(ConfigurationManager.AppSettings["OTPExpired"],
                            ConfigurationManager.AppSettings["OTPExpired"]);
                    }
                    response.Close();
                }
                #endregion
                #region Get SSO Token
                DateTime requestTime = DateTime.UtcNow;
                string SSOUrl = ConfigurationManager.AppSettings["SSOUrl"];
                string post = "";
                BuildPostLoad(out post, null);
                byte[] encodedPost = System.Text.Encoding.UTF8.GetBytes(post);
                response = MakeHttpRequest(_url + SSOUrl + Urlpostfix(), encodedPost, null);
                if (GetResponseString(response).Contains("Re-login with 2FA passcode"))
                {
                    Console.WriteLine("OTP Expired, Get OTP from Outlook");
                    string otp = "";
                    using (EmailHandler email = new EmailHandler())
                    {
                        otp = email.GetOTP(requestTime).ToString();
                    }
                    if(otp == "")
                    {
                        Console.WriteLine("Please enter OTP:");
                        otp = Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("OTP successfully load from Outlook");
                    }
                    response.Close();
                    BuildPostLoad(out post, otp);
                    encodedPost = System.Text.Encoding.UTF8.GetBytes(post);
                    response = MakeHttpRequest(_url + SSOUrl + Urlpostfix(), encodedPost, null);
                }
                CookieContainer collection = new CookieContainer();
                string sso = GetCookieHeader(response);
                if (!sso.Contains("iceSsoCookie")) RequestSSOCookie();
                WriteCookiesToDisk(null, sso);
                collection.SetCookies(new Uri(_url), sso);
                response.Close();
                #endregion
                Console.WriteLine("SSO Cookie is Ready");
                return collection;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                System.Windows.Forms.Application.Exit();
            }
            return null;

        }

        private string GetCookieHeader(HttpWebResponse response)
        {
            return response.Headers.Get("Set-Cookie"); ;
        }

        public void WriteCookiesToDisk(string file, string cookieJar)
        {
            if (String.IsNullOrEmpty(file)) file = _defaultCookiePath;
            try
            {
                Console.Out.Write("Writing cookies to disk... ");
                if (!File.Exists(file))
                {
                    File.WriteAllText(file, cookieJar);
                }
                Console.Out.WriteLine("Done.");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Problem writing cookies to disk: " + e.GetType());
            }
        }

        public string ReadCookiesFromDisk(string file)
        {
            if (String.IsNullOrEmpty(file)) file = _defaultCookiePath;
            if (!File.Exists(file))
            {
                Console.WriteLine("SSO Cookie does not exist, ask for OTP");
                return null;
            }
            try
            {
                return System.IO.File.ReadAllLines(file)[0];
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Problem reading cookies from disk: " + e.GetType());
                return null;
            }
        }
        #endregion
        #region Page Analyzer
        private void AnalyzeWebsite()
        {
            try
            {
                if (!Cef.IsInitialized)
                {
                    var settings = new CefSettings();
                    settings.IgnoreCertificateErrors = true; //bug fix: theice.com SSL Certificate expired
                    Cef.Initialize(settings);

                }
                var cookieManager = Cef.GetGlobalCookieManager();
                // Create the offscreen Chromium browser.
                var cookie = new CefSharp.Cookie();
                var cookies = RequestSSOCookie().GetCookies(new Uri(_url));
                string cookie_string = string.Empty;
                foreach (System.Net.Cookie cook in cookies)
                {
                    cookieManager.SetCookieAsync(_url + "reports/DealReport.shtml?",
                        ConvertCookie(cook));

                }
                var browser = new ChromiumWebBrowser(_url + "reports/DealReport.shtml?");
                browser.DownloadHandler = new DownloadHandler(this);
                browser.LoadingStateChanged += AnalyzePage;
                _pageAnalyzeFinished.WaitOne();
                browser.Dispose();
                System.Windows.Forms.Application.ExitThread();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async void AnalyzePage(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;
            if (e.IsLoading) return;
            var html = await wb.GetSourceAsync();
            if (html == "<html><head></head><body></body></html>") return;
            Console.WriteLine("Analyzing Reports");
            var scriptTask = await wb.EvaluateScriptAsync(
                "document.getElementById('tradeBeginDate').value = '"
                 + DateTime.Now.ToString("dd-MMM-yyyy") + "'");
            scriptTask = await wb.EvaluateScriptAsync(
                    "document.getElementById('tradeEndDate').value = '"
                     + DateTime.Now.ToString("dd-MMM-yyyy") + "'");
            for (int i = 0; i < 2; ++i)
            {
                int temp = i;
                scriptTask = null;
                scriptTask = await wb.EvaluateScriptAsync(string.Format(
                 "document.getElementById('companyId').options[{0}].selected = true;", temp));
                while ((await wb.EvaluateScriptAsync(string.Format(
                            "console.log(document.getElementById('companyId').options[{0}]);", temp))).Message as string == "false") ;
                scriptTask = null;
                scriptTask = await wb.EvaluateScriptAsync(
                            "document.evaluate(\"//a[contains(., 'Show')]\", document, null, XPathResult.ANY_TYPE, null ).iterateNext().click();");
                JavascriptResponse waitingTask = null;
                Console.WriteLine("Retrieving Count - " + i);
                while (string.IsNullOrEmpty((waitingTask?.Result as string)))
                {

                    waitingTask = await wb.EvaluateScriptAsync(
                                "document.getElementById('futuresCount').innerHTML;");
                    Thread.Sleep(100);
                }
                scriptTask = waitingTask;
                string f = (scriptTask.Result as string);
                futures += int.Parse(f.Substring(1, f.Length - 2));
                scriptTask = null;
                while (string.IsNullOrEmpty((scriptTask?.Result as string)))
                {
                    scriptTask = await wb.EvaluateScriptAsync(
                            "document.getElementById('clearedCount').innerHTML;");
                    Thread.Sleep(100);
                }
                f = (scriptTask.Result as string);
                swap += int.Parse(f.Substring(1, f.Length - 2));
                await SavePageScreenShot(wb, temp);
                wb.LoadingStateChanged -= AnalyzePage;
                await wb.EvaluateScriptAsync(
                    "document.getElementsByClassName('js-download-deals')[1].click();");
                while (!isDownloadCompleted) ;
                scriptTask = await wb.EvaluateScriptAsync(string.Format(
                 "document.getElementById('companyId').options[{0}].innerHTML", temp));
                RenameExcel(NameMap[NameMap.Keys.Where(key => scriptTask.Result.ToString().Contains(key)).FirstOrDefault()]);
                isDownloadCompleted = false;
                DownloadFileName = "";
            }          
            _pageAnalyzeFinished.Set();
        }

        private void RenameExcel(string name)
        {
            var excelFiles = Directory.GetFiles(ConfigurationManager.AppSettings["TempFolderPath"], "*"
                + ConfigurationManager.AppSettings["DownloadedFileType"])
                                     .Select(Path.GetFileName);
            foreach (var file in excelFiles)
            {
                if (file != DownloadFileName) continue;
                System.IO.File.Move(ConfigurationManager.AppSettings["TempFolderPath"] + file,
                    ConfigurationManager.AppSettings["TempFolderPath"] +
                    Path.GetFileNameWithoutExtension(file) + "_" + name
                     + Path.GetExtension(file));
            }

        }
        #endregion
        private async Task<object> SavePageScreenShot(ChromiumWebBrowser wb, int temp)
        {
            var task = await wb.ScreenshotAsync();
            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        string.Format("CefSharp screenshot{0}.png", temp));

            Console.WriteLine();
            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

            // Save the Bitmap to the path.
            // The image type is auto-detected via the ".png" extension.
            task.Save(screenshotPath);

            // We no longer need the Bitmap.
            // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
            task.Dispose();        

            // Tell Windows to launch the saved image.
            if (ConfigurationManager.AppSettings["EnableOpenScreenShot"]
                    == "true")
            {
                Console.WriteLine("Screenshot saved.  Launching your default image viewer...");
                System.Diagnostics.Process.Start(screenshotPath);
            }
            return Task.FromResult<object>(null);
        }

        private void OutputTo(string futures, string cleared)
        {
            string outputPath = ConfigurationManager.AppSettings["OutputPath"];
            //after your loop
            File.WriteAllText(outputPath, FormatCount(futures, cleared));

        }
        public void OutputCountToFile()
        {
            OutputTo(this.futures.ToString(), this.swap.ToString().Split("[".ToCharArray())
                                    .Where(name => !string.IsNullOrEmpty(name)).FirstOrDefault());
        }
        private string FormatCount(string futures, string cleared)
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("BOOK, TRADE_COUNT");
            csv.AppendLine(string.Format("cleared swap,{0}", cleared));
            csv.AppendLine(string.Format("future,{0}", futures));
            return csv.ToString();
        }
        private string FormatCount()
        {
            return FormatCount(this.futures.ToString(), this.swap.ToString());
        }
        private class DownloadHandler : IDownloadHandler
        {
            WebTradeMonitor drm;
            void IDownloadHandler.OnBeforeDownload(IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
            {
                if (!callback.IsDisposed)
                {
                    using (callback)
                    {
                        drm.DownloadFileName = downloadItem.SuggestedFileName;
                        callback.Continue(ConfigurationManager.AppSettings["TempFolderPath"] +
                                            downloadItem.SuggestedFileName, showDialog: false);
                    }
                }
            }

            void IDownloadHandler.OnDownloadUpdated(IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
            {
                if (downloadItem.IsComplete)
                {
                    drm.isDownloadCompleted = true;
                }
            }

            public DownloadHandler(WebTradeMonitor drm)
            {
                this.drm = drm;
            }
        }
    }

}