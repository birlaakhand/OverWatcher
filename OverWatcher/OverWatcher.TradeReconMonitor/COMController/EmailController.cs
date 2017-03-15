using System;
using System.Collections.Generic;
using Microsoft.Office.Interop.Outlook;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using OverWatcher.Common.Logging;
using System.Diagnostics;
using OverWatcher.Common.Interface;

namespace OverWatcher.TradeReconMonitor.Core
{
    internal sealed class EmailController : COMInterfaceBase
    {
        private Application _outlook;
        private readonly _NameSpace _ns = null;
        #region Thread Sync
        private AutoResetEvent syncLock = new AutoResetEvent(false);
        private MailItem awaitingMail = null;
        private delegate void WaitForNewMailDelegate(object item);
        private bool isUsingOpenedOutlook = false;
        #endregion
        public EmailController()
        {
            OpenOutlook();
            _ns = GetCOM<_NameSpace>(_outlook.GetNamespace("MAPI"));
        } 

        private void OpenOutlook()
        {
            if(Process.GetProcessesByName("OUTLOOK").Length > 0)
            {
                isUsingOpenedOutlook = true;
                _outlook = (Application)Marshal.GetActiveObject("Outlook.Application");
            }
            else
            {
                _outlook = new Application();
                isUsingOpenedOutlook = false;
            }
        }
        public void SendResultEmail(string HTMLbody, string body, List<string> attachments)
        {
            Logger.Info("Sending Result Email...");
            MailItem mailItem = null;
            try
            {
                mailItem = GetCOM<MailItem>(_outlook.CreateItem(OlItemType.olMailItem));
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
                MAPIFolder inboxFolder = GetCOM<MAPIFolder>(_ns.GetDefaultFolder(OlDefaultFolders.olFolderInbox));
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
                if (isUsingOpenedOutlook)
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
                    if(_outlook != null)
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
