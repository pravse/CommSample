using System;
using System.Collections.Generic;
using System.Linq;

using System.Net;
using System.Net.Mail;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;
using Crystalbyte.Equinox.Imap;

namespace WcfCommService
{
    /// <summary>
    /// 
    /// </summary>
    public class ImapAccountInfo
    {
        public string UserName = null;
        public string UserPwd = null;

        public string XOAuthKey = null;
        public string XOAuthSecret = null;
        public string XOAuthAppDisplayName = null;

        public ImapProvider Provider = null;
    };

    /// <summary>
    /// 
    /// </summary>
    public class SmtpAccountInfo
    {
        public string UserName = null;
        public string UserPwd = null;

        public SmtpProvider Provider = null;
    };

    /// <summary>
    /// 
    /// </summary>
    public class MailClient
    {
        #region private
        Crystalbyte.Equinox.Imap.ImapClient imapClient = null;
        SmtpClient smtpClient = null;
        #endregion

        public ImapClient Imap { get { return imapClient; } }
        public SmtpClient Smtp { get { return smtpClient; } }

        /// This method creates a mail client that connects to a well-defined IMAP server and SMTP server. 
        public MailClient(ImapAccountInfo ImapAccount, SmtpAccountInfo SmtpAccount)
        {
            imapClient = ImapAccount.Provider.InitClient(ImapAccount);
            smtpClient = SmtpAccount.Provider.InitClient(SmtpAccount);

        }
        
    } // class MailClient
}

