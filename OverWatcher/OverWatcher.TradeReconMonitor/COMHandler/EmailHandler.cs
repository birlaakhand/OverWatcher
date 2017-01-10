using System;
using System.Collections.Generic;
using Microsoft.Office.Interop.Outlook;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using OverWatcher.Common.Logging;
using System.Diagnostics;

namespace OverWatcher.TradeReconMonitor.Core
{
    class EmailHandler : COMInterfaceBase
    {
        private Application outlook;
        private _NameSpace ns = null;
        #region Thread Sync
        private AutoResetEvent syncLock = new AutoResetEvent(false);
        private MailItem awaitingMail = null;
        private delegate void WaitForNewMailDelegate(object item);
        private bool isUsingOpenedOutlook = false;
        #endregion
        public EmailHandler()
        {
            OpenOutlook();
            ns = GetCOM<_NameSpace>(outlook.GetNamespace("MAPI"));
        } 

        private void OpenOutlook()
        {
            if(Process.GetProcessesByName("OUTLOOK").Length > 0)
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
                mailItem.Subject = "ICE Openlink Trade Recon Results";
                mailItem.To = ConfigurationManager.AppSettings["EmailReceipts"];
                mailItem.HTMLBody = body + Environment.NewLine + HTMLbody;
                mailItem.Importance = OlImportance.olImportanceNormal;
                attachments?.ForEach(att => mailItem.Attachments.Add(att));
                mailItem.Send();
            }
            catch(System.Exception ex)
            {
                Logger.Error("Send Result Email Failed --" + ex);
                this.Dispose();
            }

        }
        public string GetOTP(DateTime requestTime)
        {
            string otp = "";
            try
            {
                MAPIFolder inboxFolder = GetCOM<MAPIFolder>(ns.GetDefaultFolder(OlDefaultFolders.olFolderInbox));
                inboxFolder = GetCOM<MAPIFolder>(inboxFolder.Folders[ConfigurationManager.AppSettings["OTPInboxFolder"]]);
                if (inboxFolder == null) return otp;
                inboxFolder.Items.Sort("[ReceivedTime]", true);
                MailItem mail = null;
                Logger.Info("Retreiving OTP Email...");
                for (int i = 1; i <= inboxFolder.Items.Count; ++i)
                {
                    MailItem tmp = GetCOM<MailItem>(inboxFolder.Items[i]);
                    if (tmp.ReceivedTime >= requestTime &&
                            tmp.Subject.Contains(ConfigurationManager.AppSettings["OTPEmailSubject"]))
                    {
                        mail = tmp;
                        break;
                    }
                    if (tmp.ReceivedTime < requestTime) break;
                }
                if (mail == null)
                {
                    mail = WaitForNewEmail(inboxFolder, WaitForNewOTPMail);
                }
                Regex regex = new Regex("(?<=Your ICE 2FA passcode is )[0-9]*");
                int t;
                string result = regex.Match(mail.Body).Value;
                if (int.TryParse(result, out t))
                {
                    otp = result;
                }
                mail.Delete();
                return otp;
            }
            catch(System.Exception ex)
            {
                Logger.Warn("Retreiving OTP Email Failed --" + ex.Message);
                this.Dispose();
                return otp;
            }

        }

        private MailItem WaitForNewEmail(MAPIFolder inbox, WaitForNewMailDelegate del)
        {
            var handler = new ItemsEvents_ItemAddEventHandler(del);
            inbox.Items.ItemAdd += handler;
            syncLock.WaitOne();
            inbox.Items.ItemAdd -= handler;
            MailItem item = this.awaitingMail;
            awaitingMail = null;
            return item;
        }

        private void WaitForNewOTPMail(object item)
        {
            MailItem mail = GetCOM<MailItem>(item as MailItem);
            if(mail != null && mail.Subject.Contains(ConfigurationManager.AppSettings["OTPEmailSubject"]))
            {
                awaitingMail = mail;
                syncLock.Set();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if(isUsingOpenedOutlook)
                    {
                        CloseCOM(COMCloseType.DecrementRefCount);
                        Marshal.ReleaseComObject(outlook);
                        outlook = null;
                    }
                    else
                    {
                        CloseCOM(COMCloseType.Exit);
                        outlook.Quit();
                        Marshal.FinalReleaseComObject(outlook);
                        outlook = null;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EmailHandler() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
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
