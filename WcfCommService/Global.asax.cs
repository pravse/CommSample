using System;
using System.ServiceModel.Activation;
using System.Web;
using System.Web.Routing;
using System.Configuration;

namespace WcfCommService
{
    public class Global : HttpApplication
    {
        static public MailClient GmailClient = null;
        static public DoodleClient DoodleClient = null;
        static public TwilioClient TwilioClient = null;

        void Application_Start(object sender, EventArgs e)
        {
            RegisterRoutes();

            StartGmailClient();
            StartDoodleClient();

            // TODO: would prefer to encapsulate this somewhere else, rather than here.... will come back to this
            string twiMLCallbackUrl = this.Request.Url.Scheme + "//" + this.Request.Url.Authority + this.Request.ApplicationPath + "/TwiML";
            StartTwilioClient(twiMLCallbackUrl);
        }

        private void RegisterRoutes()
        {
            // Edit the base address of Service1 by replacing the "Service1" string below
            RouteTable.Routes.Add(new ServiceRoute("CommService", new WebServiceHostFactory(), typeof(CommService)));
        }

        private void StartGmailClient()
        {
            string userName = ConfigurationManager.AppSettings["GMAIL_USERNAME"];
            string userPwd = ConfigurationManager.AppSettings["GMAIL_USERPWD"];

            if ((null == userName) || (null == userPwd))
            {
                GmailClient = null;
                return;
            }

            ImapAccountInfo imapAccount = new ImapAccountInfo();
            imapAccount.UserName = userName;
            imapAccount.UserPwd = userPwd;
            imapAccount.Provider = ImapProvider.GMail;

            SmtpAccountInfo smtpAccount = new SmtpAccountInfo();
            smtpAccount.UserName = userName;
            smtpAccount.UserPwd = userPwd;
            smtpAccount.Provider = SmtpProvider.GMail;

            GmailClient = new MailClient(imapAccount, smtpAccount);
        }

        private void StartDoodleClient()
        {
            // the Doodle client is using their test API so far, so it does need an OAuth key
            DoodleClient = new DoodleClient();
        }

        private void StartTwilioClient(string twiMLCallbackUrl)
        {
            TwilioClient = new TwilioClient(twiMLCallbackUrl);
        }
    }
}
