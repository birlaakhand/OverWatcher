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
    internal sealed class EmailNotifier : COMInterfaceBase
    {
        private Application _outlook;
        private readonly _NameSpace _ns = null;
        private bool _isUsingOpenedOutlook = false;
        public EmailNotifier()
        {
            OpenOutlook();
            _ns = GetCOM<_NameSpace>(_outlook.GetNamespace("MAPI"));
        }

        private void OpenOutlook()
        {
            if (Process.GetProcessesByName("OUTLOOK").Length > 0)
            {
                _isUsingOpenedOutlook = true;
                _outlook = (Application)Marshal.GetActiveObject("Outlook.Application");
            }
            else
            {
                _outlook = new Application();
                _isUsingOpenedOutlook = false;
            }
        }
        public void SendResultEmail(string HTMLbody, string body, List<string> attachments)
        {
            Logger.Info("Sending Result Email...");
            MailItem mailItem = null;
            try
            {
                mailItem = GetCOM<MailItem>(_outlook.CreateItem(OlItemType.olMailItem));
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
        private bool _disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                if (_isUsingOpenedOutlook)
                {
                    CloseCOM(COMCloseType.DecrementRefCount);
                    if (_outlook != null)
                    {
                        Marshal.ReleaseComObject(_outlook);
                    }
                    _outlook = null;
                }
                else
                {
                    CloseCOM(COMCloseType.Exit);
                    if (_outlook != null)
                    {
                        _outlook.Quit();
                        Marshal.FinalReleaseComObject(_outlook);
                        _outlook = null;
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                _disposedValue = true;
            }
        }
        /// <summary>
        /// Setup the COM type to be call Close()
        /// </summary>
        protected override void CleanUpSetup()
        {
            ClosableComList.Add(typeof(MailItem));
        }
        #endregion
    }
}
