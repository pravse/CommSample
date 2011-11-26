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

    public class OAuthProvider
    {

        #region GMAIL
        static string GMAIL_OAUTH_REQUESTTOKEN_ENDPOINT = "https://www.google.com/accounts/OAuthGetRequestToken";
        static string GMAIL_OAUTH_AUTHTOKEN_ENDPOINT = "https://www.google.com/accounts/OAuthAuthorizeToken";
        static string GMAIL_OAUTH_ACCESSTOKEN_ENDPOINT = "https://www.google.com/accounts/OAuthGetAccessToken";
        static string GMAIL_OAUTH_REQUEST_SCOPE = "https://mail.google.com/";
        static string GMAIL_OAUTH_URLFORMAT = "https://mail.google.com/mail/b/{0}/imap/";

        public static OAuthProvider GMail = new OAuthProvider(GMAIL_OAUTH_REQUESTTOKEN_ENDPOINT,
                                                              GMAIL_OAUTH_AUTHTOKEN_ENDPOINT,
                                                              GMAIL_OAUTH_ACCESSTOKEN_ENDPOINT,
                                                              GMAIL_OAUTH_REQUEST_SCOPE,
                                                              GMAIL_OAUTH_URLFORMAT);

        #endregion

        public string XOAuthRequestTokenEndPoint = null;
        public string XOAuthAuthTokenEndPoint = null;
        public string XOAuthAccessTokenEndPoint = null;
        public string XOAuthRequestScope = null;
        public string XOAuthUrlFormat = null;

        public OAuthProvider(
            string requestTokenEP,
            string authTokenEP,
            string accessTokenEP,
            string requestScope,
            string urlFormat)
        {
            XOAuthRequestTokenEndPoint = requestTokenEP;
            XOAuthAuthTokenEndPoint = authTokenEP;
            XOAuthAccessTokenEndPoint = accessTokenEP;
            XOAuthRequestScope = requestScope;
            XOAuthUrlFormat = urlFormat;
        }

        // needed as a callback if IMAP uses OAuth (not necessary if username/pwd will be provided)
        public void OAuthHandler(ImapClient client,
                 NetworkCredential credential,
                 ImapAccountInfo account)
        {
            if (!client.ServerCapability.Items.Contains("AUTH=XOAUTH"))
            {
                return;
            }

            if (string.IsNullOrEmpty(account.XOAuthKey))
            {
                // in this case the username is the email address;
                var email = credential.UserName;

                var token = new OAuthRequest().
                    WithAnonymousConsumer().
                    WithEndpoint(XOAuthRequestTokenEndPoint).
                    WithParameter("scope", XOAuthRequestScope).
                    WithParameter(OAuthParameters.OAuthCallback, "oob").
                    WithParameter("xoauth_displayname", account.XOAuthAppDisplayName).
                    WithSignatureMethod(OAuthSignatureMethods.HmacSha1).
                    Sign().
                    RequestToken();

                var authUrl = new OAuthRequest().
                    WithEndpoint(XOAuthAuthTokenEndPoint).
                    WithToken(token).
                    GetAuthorizationUri();

                // TODO: change this to (a) redirect the user to the authUri, 
                // (b) get back the verification code in a callback from the authUri
                //
                // Probably means we have to break this method into at least two different methods
                // with some sort of flow between them.
                //
                account.XOAuthSecret = GetXOAuthSecret(authUrl);

                var accessToken = new OAuthRequest().
                    WithAnonymousConsumer().
                    WithEndpoint(XOAuthAccessTokenEndPoint).
                    WithParameter(OAuthParameters.OAuthVerifier, account.XOAuthSecret).
                    WithSignatureMethod(OAuthSignatureMethods.HmacSha1).
                    WithToken(token).
                    Sign().
                    RequestToken();

                var authUri = String.Format(XOAuthUrlFormat, email);
                account.XOAuthKey = new OAuthRequest().
                    WithAnonymousConsumer().
                    WithEndpoint(authUri).
                    WithSignatureMethod(OAuthSignatureMethods.HmacSha1).
                    WithToken(accessToken).
                    Sign().
                    CreateXOAuthKey();
            }

            client.AuthenticateXOAuth(account.XOAuthKey);
        }

        // TODO: pravse: needs to be implemented correctly (see note earlier)
        //
        private string GetXOAuthSecret(System.Uri authUri)
        {
            /**** 
                  Process.Start(authUrl.AbsoluteUri);
	       
                  string verificationCode;
                  using (var form = new OAuthVerificationForm()) {
                  var result = form.ShowDialog();
                  if (result == DialogResult.Cancel) {
                  return;
                  }
                  verificationCode = form.VerificationCode;
                  }
            ***/
            return null;
        }
    }

}