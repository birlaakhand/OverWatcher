using CefSharp;
using CefSharp.OffScreen;
using OverWatcher.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverWatcher.Common.CefSharpBase
{
    public static class BrowserWatcherHelper
    {
        public static async Task<object> SavePageScreenShot(this ChromiumWebBrowser wb, string path)
        {
            var task = await wb.ScreenshotAsync();
            Logger.Info(string.Format("Screenshot ready. Saving to {0}", path));

            // Save the Bitmap to the path.
            // The image type is auto-detected via the ".png" extension.
            task.Save(path);

            // We no longer need the Bitmap.
            // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
            task.Dispose();
#if DEBUG
            // Tell Windows to launch the saved image.
            Logger.Info("Screenshot saved.  Launching your default image viewer...");
            System.Diagnostics.Process.Start(path);
#endif
            return Task.FromResult<object>(null);
        }

        public static Task<JavascriptResponse> EvaluateXPathScriptAsync(this ChromiumWebBrowser wb, string xpath, string action)
        {
            return wb.EvaluateScriptAsync(
                string.Format("document.evaluate(\"{0}\", document, null, XPathResult.ANY_TYPE, null ).iterateNext(){1}", xpath, action));
        }
    }
}
