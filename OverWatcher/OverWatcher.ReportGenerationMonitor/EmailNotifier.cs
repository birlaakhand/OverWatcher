using Microsoft.Office.Interop.Outlook;
using OverWatcher.Common.Interface;
using OverWatcher.Common.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OverWatcher.ReportGenerationMonitor
{
    class EmailNotifier : COMInterfaceBase
    {
        private Application outlook;
        private _NameSpace ns = null;
        private bool isUsingOpenedOutlook = false;
        public EmailNotifier()
        {
            OpenOutlook();
            ns = GetCOM<_NameSpace>(outlook.GetNamespace("MAPI"));
        }

        private void OpenOutlook()
        {
            if (Process.GetProcessesByName("OUTLOOK").Length > 0)
            {
                isUsingOpenedOutlook = true;
                outlook = (Application)Marshal.GetActiveObject("Outlook.Application");
            }
            else
            {
                outlook = new Application();
                isUsingOpenedOutlook = false;
            }
        }
        public void SendResultEmail(string HTMLbody, string body, List<string> attachments)
        {
            Logger.Info("Sending Result Email...");
            MailItem mailItem = null;
            try
            {
                mailItem = GetCOM<MailItem>(outlook.CreateItem(OlItemType.olMailItem));
                mailItem.Subject = "B-Brent Crude Future Report Generation Time";
                mailItem.To = ConfigurationManager.AppSettings["EmailReceipts"];
                mailItem.HTMLBody = body + Environment.NewLine + HTMLbody;
                mailItem.Importance = OlImportance.olImportanceNormal;
                attachments?.ForEach(att => mailItem.Attachments.Add(att));
                mailItem.Send();
            }
            catch (System.Exception ex)
            {
                Logger.Error("Send Result Email Failed --" + ex);
                this.Dispose();
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                if (isUsingOpenedOutlook)
                {
                    CloseCOM(COMCloseType.DecrementRefCount);
                    if (outlook != null)
                    {
                        Marshal.ReleaseComObject(outlook);
                    }
                    outlook = null;
                }
                else
                {
                    CloseCOM(COMCloseType.Exit);
                    if (outlook != null)
                    {
                        outlook.Quit();
                        Marshal.FinalReleaseComObject(outlook);
                        outlook = null;
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                disposedValue = true;
            }
        }
        /// <summary>
        /// Setup the COM type to be call Close()
        /// </summary>
        protected override void CleanUpSetup()
        {
            closableCOMList.Add(typeof(MailItem));
        }
        #endregion
    }
}
