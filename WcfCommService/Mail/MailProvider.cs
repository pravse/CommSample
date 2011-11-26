using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;
using Crystalbyte.Equinox.Imap;

namespace WcfCommService
{
    public delegate void MessageCallbackDelegate(string msgSubject, string msgText);

    public class ImapReader
    {
        private MessageCallbackDelegate callbackFn;
        private string                  folderName;
        private ImapClient              imapClient;
        private int?                    uidNext;

        // class used as a container to extract message fields via a LINQ Select() clause
        class InitialResponseFields
        {
            public Envelope                     Envelope;
            public Crystalbyte.Equinox.Size     Size;
            public DateTime                     InternalDate;
            public int                          Uid;
            public MessageInfo                  BodyStructure;
        }

        public delegate void IdlerDelegate();

        public ImapReader(ImapClient client, string folder, MessageCallbackDelegate cb)
        {
            callbackFn = cb;
            folderName = folder;
            imapClient = client;

            // Step 1: add delegate to the 
            imapClient.StatusUpdateReceived += onStatusUpdateReceived; 
            Crystalbyte.Equinox.Imap.Responses.SelectExamineImapResponse selectResponse = imapClient.Select("INBOX");

            // record the next uid to read ---- this ensures that we will only read new messages
            uidNext = selectResponse.MailboxInfo.UidNext;

            /***
            // let's just check that we can get messages
            var query = client.Messages;
            foreach (var msg in query)
            {
                if (msg.Subject.Contains("bot"))
                {
                    callbackFn(msg.Text);
                }
            }
             * ***/

            IdlerDelegate asyncIdleMethod = startIdle;

            asyncIdleMethod.BeginInvoke(startIdleAsyncCallback, asyncIdleMethod);
            
        }

        /// On the IMAP server, the client enters IDLE mode (thereby occupying the invoking thread). 
        /// Consequently, this needs to be invoked on its own thread. 
        /// The ImapProvider handles this by using asynchronous invocation.
        private void startIdle()
        {
            imapClient.StartIdle();
        }

        /// <summary>
        ///  this should never really get called, because the IDLE state should never be released, I think.
        /// </summary>
        /// <param name="result"></param>
        private void startIdleAsyncCallback(IAsyncResult ar)
        {
            IdlerDelegate dlgt = (IdlerDelegate)ar.AsyncState;

            // Complete the call.
            dlgt.EndInvoke(ar);
        }

        // Respond to change notifications
        private void onStatusUpdateReceived(object sender, StatusUpdateReceivedEventArgs e)
        {    
            var client = sender as ImapClient;
            if (client == null) 
            {
                return;
            }
      
            var since = DateTime.Now.AddDays(-7);
            var query = client.Messages.
                        Where(x => x.InternalDate > since && x.Uid >= uidNext).
                        Select(x => new InitialResponseFields { 
                                Uid = x.Uid,
                                Envelope = x.Envelope,
                                Size = x.Size,
                                InternalDate = x.InternalDate,
                                BodyStructure = x.BodyStructure});

            foreach (var msg in query)
            {
                uidNext = msg.Uid + 1;
                if (msg.Envelope.Subject.Contains("bot") && msg.Envelope.Subject.Contains("[id="))
                {
                    try
                    {
                        // get the text/html view
                        ViewInfo htmlViewInfo = msg.BodyStructure.Views.Where(x => x.MediaType == "text/html").First<ViewInfo>();
                        if (null != htmlViewInfo) {
                            // back to the server to retrieve the text/html body part
                            Crystalbyte.Equinox.View htmlView = client.FetchView(htmlViewInfo);
                            if (null != htmlView) {
                                callbackFn(msg.Envelope.Subject, htmlView.Text);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            /** Keep the IDLE state going, so that it is always reading new mail....
            if (e.IsIdleUpdate) 
            {
                e.IsIdleCancelled = true; // cancel IDLE session (false default)
            }
             * ***/
        }
    }

    public class ImapProvider
    {

        #region private
        string mailHost;
        int mailPort;
        // if there needs to be OAuth
        OAuthProvider oAuthProvider = null;
        #endregion

        public ImapProvider(string hostName, int hostPort, OAuthProvider provider)
        {
            mailHost = hostName;
            mailPort = hostPort;
            oAuthProvider = provider;
        }

        public string Host { get { return mailHost; } }
        public int Port { get { return mailPort; } }

        public void AuthHandler(ImapClient client, NetworkCredential credential, ImapAccountInfo account)
        {
            if (null != oAuthProvider)
            {
                oAuthProvider.OAuthHandler(client, credential, account);
            }
        }

        public ImapClient InitClient(ImapAccountInfo account)
        {
            Debug.Assert(null != account);

            ImapClient imapClient = new Crystalbyte.Equinox.Imap.ImapClient();
            imapClient.Security = SecurityPolicies.Explicit;

            imapClient.Connect(this.Host, this.Port);

            if ((null != account.UserName) && (null != account.UserPwd))
            {
                // doesn't need OAuth --- try regular LOGIN
                imapClient.Authenticate(account.UserName, account.UserPwd, SaslMechanics.Login);
            }
            else
            {
                imapClient.ManualSaslAuthenticationRequired += (sender, e) => this.AuthHandler(e.Client, e.UserCredentials, account);
                imapClient.Authenticate(account.UserName, account.UserPwd);
            }
            return imapClient;
        }


        #region GMAIL
        static string GMAIL_HOSTNAME = "imap.gmail.com";
        static int GMAIL_PORT = 993;

        public static ImapProvider GMail = new ImapProvider(GMAIL_HOSTNAME, GMAIL_PORT, OAuthProvider.GMail);
        #endregion
    }

    public class SmtpProvider
    {

        #region private
        string mailHost;
        int mailPort;
        bool enableSsl;
        #endregion

        public SmtpProvider(string hostName, int hostPort, bool isSsl)
        {
            mailHost = hostName;
            mailPort = hostPort;
            enableSsl = isSsl;
        }

        public string Host { get { return mailHost; } }
        public int Port { get { return mailPort; } }
        public bool EnableSsl { get { return enableSsl; } }

        public SmtpClient InitClient(SmtpAccountInfo account)
        {
            SmtpClient smtpClient = new SmtpClient(account.Provider.Host);
            smtpClient.Port = account.Provider.Port;
            smtpClient.EnableSsl = account.Provider.EnableSsl;

            smtpClient.Credentials = new System.Net.NetworkCredential(account.UserName, account.UserPwd);
            return smtpClient;
        }


        #region GMAIL
        static string GMAIL_HOSTNAME = "smtp.gmail.com";
        static int GMAIL_PORT = 587;

        public static SmtpProvider GMail = new SmtpProvider(GMAIL_HOSTNAME, GMAIL_PORT, true);
        #endregion
    }
}