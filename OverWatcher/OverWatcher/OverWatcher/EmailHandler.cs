using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Outlook;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;
using System.Data;
using OverWatcher.TheICETrade;

namespace OverWatcher
{
    class EmailHandler : IDisposable
    {
        private Application outlook;
        private _NameSpace ns = null;
        #region Thread Sync
        private AutoResetEvent syncLock = new AutoResetEvent(false);
        private MailItem awaitingMail = null;
        private delegate void WaitForNewMailDelegate(object item);
        #endregion
        public EmailHandler()
        {
            outlook = new Application();
            ns = outlook.GetNamespace("MAPI");

        } 

        public void SendResultEmail(string body, List<string> attachments)
        {
            Console.WriteLine("Sending Result Email...");
            MailItem mailItem = null;
            try
            {
                mailItem = outlook.CreateItem(OlItemType.olMailItem);
                mailItem.Subject = "ICE Oracle Monitor Result";
                mailItem.To = ConfigurationManager.AppSettings["EmailReceipts"];
                mailItem.Body = body;
                mailItem.Importance = OlImportance.olImportanceNormal;
                attachments?.ForEach(att => mailItem.Attachments.Add(att));
                mailItem.Display(false);
                mailItem.Send();
                mailItem.Close(OlInspectorClose.olDiscard);
                Marshal.FinalReleaseComObject(mailItem);
            }
            catch(System.Exception ex)
            {
                Console.WriteLine("Send Result Email Failed --" + ex.Message);
                this.Dispose();
            }

        }
        public string GetOTP(DateTime requestTime)
        {
            string otp = "";
            try
            {
                MAPIFolder inboxFolder = ns.GetDefaultFolder(OlDefaultFolders.olFolderInbox)?
                            .Folders[ConfigurationManager.AppSettings["OTPInboxFolder"]];
                if (inboxFolder == null) return otp;
                inboxFolder.Items.Sort("[ReceivedTime]", true);
                MailItem mail = null;
                Console.WriteLine("Retreiving OTP Email...");
                for (int i = 1; i <= inboxFolder.Items.Count; ++i)
                {
                    MailItem tmp = inboxFolder.Items[i] as MailItem;
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
                mail.Close(OlInspectorClose.olDiscard);
                Marshal.FinalReleaseComObject(mail);
                return otp;
            }
            catch(System.Exception ex)
            {
                Console.WriteLine("Retreiving OTP Email Failed --" + ex.Message);
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
            MailItem mail = item as MailItem;
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
                    outlook.Quit();
                    Marshal.FinalReleaseComObject(outlook);
                    outlook = null;

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
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
