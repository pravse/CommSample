using System;
using System.Collections.Generic;
using System.Linq;

using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Hosting;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Text.RegularExpressions;

namespace WcfCommService
{
    public delegate void ResponseCallback(ServiceResponse response);

    // here is where the logic of the service sits
    public class CommLogic
    {
        public CommLogic(MailClient mClient,
                         DoodleClient dClient,
                         TwilioClient tClient,
                         ResponseCallback callback)
        {
            mailClient = mClient;
            doodleClient = dClient;
            twilioClient = tClient;
            callbackFn = callback;

            // Start reading email from the appropriate IMap provider.
            // This creates its own long-running thread. 
            imapReader = new ImapReader(mailClient.Imap, "INBOX", MessageCallback);
        }

        public void MessageCallback(string msgSubject, string msgHtmlText)
        {
            // parse the mail message to construct the ServiceResponse
            Debug.Assert(null != msgHtmlText);
            ServiceResponse response = constructResponse(msgSubject, msgHtmlText);

            if (null != response)
            {
                // invoke the callback fn
                callbackFn(response);
            }
        }

        public RequestHandle HandleNewRequest(ServiceRequest request)
        {
            Debug.Assert(null != request);
            if (request.Channel == ChannelChoice.EMAIL_EMAIL)
            {
                return twoWayEmail(request);
            }
            else if (request.Channel == ChannelChoice.EMAIL_DOODLE)
            {
                return emailToDoodle(request);
            }
            else if (request.Channel == ChannelChoice.TWILIO)
            {
                return twilioComm(request);
            }
            return null;
        }

        // check right now for a response --- valid for channels where the response is remote without
        // a real-time notification/callback (like Doodle)
        public void CheckResponseImmediate(string requestId, 
                                           ChannelChoice channel,
                                           string requestMetadata,
                                           ResponseCallback callbackFn)
        {
            Debug.Assert(null != requestId);
            if (channel == ChannelChoice.EMAIL_DOODLE)
            {
                string answer = doodleClient.GetPollResult(requestId, requestMetadata);
                if (null != answer)
                {
                    ServiceResponse response = new ServiceResponse();
                    response.Id = requestId;
                    response.Truthful = true;
                    response.Answer = answer;
                    callbackFn(response);
                }
            }
        }

        #region private
        private MailClient mailClient;
        private DoodleClient doodleClient;
        private TwilioClient twilioClient;
        private ResponseCallback callbackFn;
        private ImapReader imapReader;

        static private string EMAIL_FORM_SUFFIX_KEY = "EMAIL_FORM_SUFFIX";
        static private string QUESTION_KEY = "QUESTION";
        static private string DOODLE_LINK_KEY = "DOODLE_LINK";

        private ServiceResponse constructResponse(string msgSubject, string msgBody)
        {
            string responseId = null;
            int subjectIdIndex = msgSubject.IndexOf("[id=");
            if (subjectIdIndex >= 0)
            {
                string partialId = msgSubject.Substring(subjectIdIndex + 4);
                int subjectIdEndIndex = partialId.IndexOf("]");
                if (subjectIdEndIndex >= 0)
                {
                    responseId = partialId.Substring(0, subjectIdEndIndex);
                }
            }

            if (null == responseId)
            {
                return null;
            }

            ServiceResponse response = new ServiceResponse();
            response.Id = responseId;
            response.Truthful = (findAnswer(msgBody, "Truthful").ToLower() == "Yes") ? true : false;
            response.Answer = findAnswer(msgBody, "Answer");
            return response;
        }

        private string findAnswer(string htmlBody, string elementId)
        {
            try
            {
                /**** Html is not XML --- so this route doesn't work --- sigh.
                 * 
                // look for an element that looks like this .... <p class="comm_answer" id="element_id">value</p>
                // and extract the value from it
                XPathDocument xpathDoc = new XPathDocument(new XmlTextReader(htmlBody, XmlNodeType.DocumentFragment, null));
                XPathNavigator docNav = xpathDoc.CreateNavigator();
                docNav = docNav.SelectSingleNode(String.Format("//p [contains(@class, 'comm_answer') and contains(@comm_id, '{0}')]/node()", elementId));
                if (null != docNav)
                {
                    return docNav.Value;
                }
                else
                {
                    return null;
                }
                 * ****/
                // could use an open-source HTML parser, but for now, just using Regex
                // TODO: pravse : still buggy -- doesn't find the element correctly
                Regex expression = new Regex(String.Format(@"id=.*{0}.*\>.*?\</p>", "\"" + elementId + "\""));
                Match regexMatch = expression.Match(htmlBody);
                if (regexMatch.Success)
                {
                    return regexMatch.Groups[1].Value;
                }
                else
                {
                    return "Not Found";
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        private string generateNewId()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }

        private string sendEmailMessage(ServiceRequest request, Dictionary<string, string> parameters)
        {
            // for two-way email, we can generate random IDs
            string requestId = generateNewId();

            // construct a mail message from the service request
            MailMessage newMsg = new MailMessage(
                mailClient.Smtp.Credentials.GetCredential(mailClient.Smtp.Host,
                                                          mailClient.Smtp.Port,
                                                          "SSL").UserName,
                request.Recipient);
            newMsg.Subject = request.Subject + "[id=" + requestId + "]";
            newMsg.Body = constructEmailMsgBody(request, parameters);
            newMsg.IsBodyHtml = true;

            // send the mail message : for now, keep it simple with a synchronous send. Make it async later
            mailClient.Smtp.Send(newMsg);

            return requestId;
        }

        private RequestHandle twoWayEmail(ServiceRequest request)
        {
            Debug.Assert(null != request);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(QUESTION_KEY, request.Question);
            parameters.Add(EMAIL_FORM_SUFFIX_KEY, "TwoWayEmail");
            return new RequestHandle
            {
                Id = sendEmailMessage(request, parameters),
                Channel = request.Channel,
                Metadata = null
            };
        }

        private RequestHandle emailToDoodle(ServiceRequest request)
        {
            // set up a Doodle poll
            CreatePollResponse createPollResponse = createDoodlePoll(request);
            string doodlePollUrl; 

            if (null == createPollResponse)
            {
                // TODO: handle this: for now, continue with a dummy url
                doodlePollUrl = doodleClient.ConstructUrl("ErrorPollId");
            }
            else
            {
                doodlePollUrl = doodleClient.ConstructUrl(createPollResponse.PollId);
            }

            // send an email message with the link to the poll
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add(DOODLE_LINK_KEY, doodlePollUrl);
            parameters.Add(EMAIL_FORM_SUFFIX_KEY, "EmailToDoodle");
            sendEmailMessage(request, parameters);

            return new RequestHandle { Id = createPollResponse.PollId, 
                                       Channel = request.Channel, 
                                       Metadata = createPollResponse.PollKey }; 
        }

        private RequestHandle twilioComm(ServiceRequest request)
        {
    	    // start a phone call to the request recipient
	        twilioClient.MakeCall(request.Recipient, request.Subject, request.Question);

            return null;
        }

        private string constructEmailMsgBody(ServiceRequest request, Dictionary<string, string> parameters)
        {
            string msgBody;
            string emailFormSuffix = parameters[EMAIL_FORM_SUFFIX_KEY];
            Debug.Assert(null != emailFormSuffix);

            string myFormFilename = HostingEnvironment.MapPath(@"/App_Data/EmailForm." + emailFormSuffix + ".htm");
            if (null != myFormFilename)
            {
                // open the file
                msgBody = File.ReadAllText(myFormFilename);
                if (null != msgBody) 
                {
                    foreach (var entry in parameters)
                    {
                        // TODO: pravse: yes it is grungy to use the parameters to also pass the form name.
                        msgBody = msgBody.Replace("%%"+entry.Key+"%%", entry.Value);    
                    }
                }
            }
            else
            {
                msgBody = request.Question;
            }
            return msgBody;
        }

        /// <summary>
        /// returns an ID for the constructed Doodle poll
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private CreatePollResponse createDoodlePoll(ServiceRequest request)
        {
            PollType newPoll = new PollType(PollTypeType.TEXT, false, "Please answer this!", "2", request.Question);
            newPoll.initiator = new InitiatorType();
            newPoll.initiator.name = "Praveen";
            newPoll.options = new OptionsTypeOption[3];
            newPoll.options[0] = new OptionsTypeOption();
            newPoll.options[0].Value = "Three?";
            newPoll.options[1] = new OptionsTypeOption();
            newPoll.options[1].Value = "Four?";
            newPoll.options[2] = new OptionsTypeOption();
            newPoll.options[2].Value = "Five?";

            CreatePollResponse createPollResponse = doodleClient.CreatePoll(newPoll);

            return createPollResponse; ;
        }
        #endregion
    }
}

