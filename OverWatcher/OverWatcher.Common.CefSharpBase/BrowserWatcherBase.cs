using CefSharp;
using CefSharp.OffScreen;
using OverWatcher.Common.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OverWatcher.Common.CefSharpBase
{
    public abstract class BrowserWatcherBase
    {
        #region Thread Share Fields
        protected volatile bool isDownloadCompleted = false;
        protected volatile string DownloadFileName = "";
        protected AutoResetEvent _pageAnalyzeFinished = new AutoResetEvent(false);
        protected readonly string TempFolderPath;
        protected BrowserWatcherBase(string temp)
        {
            TempFolderPath = temp;
        }

        #endregion
        protected string _defaultCookiePath = ConfigurationManager.AppSettings["CookiePath"];
        public static void InitializeEnvironment()
        {
            if (!Cef.IsInitialized)
            {
                var settings = new CefSettings();
                settings.IgnoreCertificateErrors = true; //bug fix: theice.com SSL Certificate expired
                settings.BrowserSubprocessPath = "./bin/CefSharp/CefSharp.BrowserSubprocess.exe";
                settings.LogFile = "./log/cefLog.log";
                Cef.Initialize(settings);

            }
        }
        public static void CleanupEnvironment()
        {
            Cef.Shutdown();
        }

        protected abstract Task StartBrowser();
        public Task RunAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(StartBrowser());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public void Run()
        {
            RunAsync().Wait();
        }
        protected CefSharp.Cookie ConvertCookie(System.Net.Cookie cookie)
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

        protected HttpWebResponse MakeHttpRequest(string url, byte[] encodedPost, CookieContainer container)
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

        protected string GetResponseString(HttpWebResponse response)
        {
            var dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseString = reader.ReadToEnd();
            dataStream.Close();
            response.Close();
            return responseString;
        }

        protected string GetCookieHeader(HttpWebResponse response)
        {
            return response.Headers.Get("Set-Cookie");
        }

        protected async Task<bool> IsPageLoading(ChromiumWebBrowser wb, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading) return true;
            var html = await wb.GetSourceAsync();
            if (html == "<html><head></head><body></body></html>") return true;
            return false;
        }
        protected void WriteCookiesToDisk(string file, string cookieJar)
        {
            if (String.IsNullOrEmpty(file)) file = _defaultCookiePath;
            try
            {
                Logger.Info("Writing cookies to disk... ");
                if (!File.Exists(file))
                {
                    File.WriteAllText(file, cookieJar);
                }
                Logger.Info("Done.");
            }
            catch (Exception e)
            {
                Logger.Warn("Problem writing cookies to disk: " + e.GetType());
            }
        }

        protected string ReadCookiesFromDisk(string file)
        {
            if (String.IsNullOrEmpty(file)) file = _defaultCookiePath;
            if (!File.Exists(file))
            {
                Logger.Info("SSO Cookie does not exist, ask for OTP");
                return null;
            }
            try
            {
                return System.IO.File.ReadAllLines(file)[0];
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Problem reading cookies from disk: ");
                return null;
            }
        }

        /// <summary>
        /// Nested class to handle download, no need to change
        /// </summary>
        protected class DownloadHandler : IDownloadHandler
        {
            private readonly BrowserWatcherBase drm;
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

            public DownloadHandler(BrowserWatcherBase drm)
            {
                this.drm = drm;
            }
        }
    }
}
