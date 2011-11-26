using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Text;
using System.IO;

namespace WcfCommService
{
    /// <summary>
    /// 
    /// </summary>
    public class TwilioClient
    {
        #region private

        private static string baseApiUrl = "https://api.twilio.com/2010-04-01";
        private static string ACCOUNT_SID_KEY = "TWILIO_ACCOUNT_SID";
        private static string AUTH_TOKEN_KEY = "TWILIO_AUTH_TOKEN";
        private static string APPLICATION_SID_KEY = "TWILIO_APPLICATION_SID";
        private static string OUTGOING_PHONE_KEY = "TWILIO_OUTGOING_PHONE";

        private string accountSid;
        private string authToken;
        private string applicationSid;
        private string outgoingPhoneNumber;
        private string twiMLCallbackUrl;

        private string getPostData(IDictionary<string, string> parameters)
        {
            string retValue = "";
            int paramCount = 0;
            if (null == parameters) return retValue;

            foreach (var entry in parameters)
            {
                if (0 == paramCount)
                {
                    retValue += entry.Key + "=" + entry.Value;
                }
                else
                {
                    retValue += "&" + entry.Key + "=" + entry.Value;
                }
                paramCount++;
            }
            return retValue;
        }

        #endregion

        public TwilioClient(string twiMLCallback)
        {
            accountSid = ConfigurationManager.AppSettings[ACCOUNT_SID_KEY];
            authToken = ConfigurationManager.AppSettings[AUTH_TOKEN_KEY];
            applicationSid = ConfigurationManager.AppSettings[APPLICATION_SID_KEY];
            outgoingPhoneNumber = ConfigurationManager.AppSettings[OUTGOING_PHONE_KEY];
            twiMLCallbackUrl = twiMLCallback;

            Debug.Assert((null != accountSid) && 
                         (null != authToken) && 
                         (null != applicationSid) && 
                         (null != outgoingPhoneNumber) &&
                         (null != twiMLCallbackUrl));
        }

        public void MakeCall(string toPhoneNumber, string subject, string question)
        {
            // TODO: verify that the ToPhoneNumber is valid

            // create a POST request
            WebRequest postRequest = WebRequest.Create(baseApiUrl + "/Accounts/" + accountSid + "/Calls");
            postRequest.ContentType = "application/x-www-form-erlencoded";
            postRequest.Method = "POST";
            // pass basic auth information: username == accountSid, pwd == authToken
            postRequest.Credentials = new NetworkCredential(accountSid, authToken);
            /*** Using Credentials for basicauth will not send the auth info on the first request. If the service returns 
             * an HTTP 401, then it resends with the auth info. But if the listening service doesn't do this, then
             * we are in trouble. In that case, use the code below to explicitly send a basic auth header
            byte[] authBytes = Encoding.UTF8.GetBytes("user:password".ToCharArray());
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
             * ***/

            // Pass the following parameters:
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("From", outgoingPhoneNumber);
            parameters.Add("To", toPhoneNumber);
            parameters.Add("Url", twiMLCallbackUrl);

            string postData = HttpUtility.UrlEncode(getPostData(parameters));
            byte[] byteBuffer = Encoding.UTF8.GetBytes(postData);

            // set the content length before opening the stream
            postRequest.ContentLength = byteBuffer.Length;
            Stream postStream = postRequest.GetRequestStream();
            postStream.Write(byteBuffer, 0, byteBuffer.Length);
            postStream.Close();


            HttpWebResponse postResponse = null;
            try
            {
                // now we send the HTTP POST request
                postResponse = (HttpWebResponse)(postRequest.GetResponse());
            }
            catch (Exception ex)
            {
                // bad HTTP requests will throw an exception
            }

            if ((null != postResponse) && (HttpStatusCode.OK == postResponse.StatusCode))
            {
                // parse the response and determine what to do with it
            }
            else
            {
                // some error response
            }
        }
    }

}

