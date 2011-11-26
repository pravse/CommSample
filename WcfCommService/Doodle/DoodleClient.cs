using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;



namespace WcfCommService
{

    public class CreatePollResponse
    {
        public string PollId;  /// 16-character id
        public string PollKey; /// 8-character key
        public string PollUrl; /// url to access this poll
    }
    
    /// <summary>
    /// 
    /// </summary>
    public class DoodleClient
    {
        static private string baseApiUrl = "http://doodle-test.com/api1WithoutAccessControl";
        static private string basePollUrl = "http://www.doodle-test.com";
        static private string baseSchemaUrl = "http://doodle.com/xsd1";

        // return content length
        private byte[] serializePoll(PollType poll)
        {
            System.IO.MemoryStream memStream = new System.IO.MemoryStream();
            XmlTextWriter xtWriter = new XmlTextWriter(memStream, Encoding.UTF8);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(PollType));
            xmlSerializer.Serialize(xtWriter, poll);
            xtWriter.Flush();

            return memStream.ToArray();
        }

        private PollType deserializePoll(Stream contentStream)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(PollType));
            PollType poll = (PollType)(xmlSerializer.Deserialize(contentStream));
            return poll;
        }

        public CreatePollResponse CreatePoll(PollType newPoll)
	    {
	        Debug.Assert(null != newPoll);

            // create a POST request
            WebRequest postRequest = WebRequest.Create(baseApiUrl + "/polls");
            postRequest.ContentType = "application/xml";
            postRequest.Method = "POST";
            byte[] byteBuffer = serializePoll(newPoll);
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

            if ((null != postResponse) && (HttpStatusCode.Created == postResponse.StatusCode))
            {
                // Successfully created .... now get the details
                string pollId = postResponse.Headers["Content-Location"];
                string pollKey = postResponse.Headers["X-DoodleKey"];
                string pollUrl = basePollUrl + "/" + pollId;

                return new CreatePollResponse { PollId = pollId, PollKey = pollKey, PollUrl = pollUrl };
            }
            else
            {
                // Error of some form. TODO: throw appropriate exceptions
                return null;
            }

	    }

        public string GetPollResult(string pollId, string pollKey)
        {
            Debug.Assert(null != pollId);

            // create a GET request
            WebRequest getRequest = WebRequest.Create(baseApiUrl + "/polls/" + pollId);

            getRequest.Method = "GET";
            if (null != pollKey)
            {
                getRequest.Headers["X-DoodleKey"] = pollKey;
            }

            HttpWebResponse getResponse = null;
            try
            {
                // now we send the HTTP GET request
                getResponse = (HttpWebResponse)(getRequest.GetResponse());
            }
            catch (Exception ex)
            {
                // bad HTTP requests will throw an exception
            }

            if ((null != getResponse) && (HttpStatusCode.OK == getResponse.StatusCode))
            {
                // Successful deserialize the xml into a poll
                // 
                Stream getStream = getResponse.GetResponseStream(); 
                // deserialize into a new PollType object
                PollType poll = deserializePoll(getStream);
                getStream.Close();
                Debug.Assert(null != poll);

                // check if there is exactly one participant -- if so, the person has responded 
                if (1 == poll.participants.participant.Length)
                {
                    ParticipantType participant = poll.participants.participant[0];
                    Debug.Assert(null != participant);
                    // go thru the options and figure out which option the participant chose
                    int chosenOption = 0;
                    foreach (string choice in participant.preferences)
                    {
                        if ("1" == choice) break; 
                        chosenOption++;
                    }
                    if (chosenOption < participant.preferences.Length)
                    {
                        // get the option value corresponding to the chosen option
                        return poll.options[chosenOption].Value;
                    }
                    // no option chosen
                    return null;
                }
                // no participant response
                return null;
            }
            else
            {
                // Error of some form. TODO: throw appropriate exceptions
                return null;
            }

        }

        public string ConstructUrl(string pollId)
        {
            return basePollUrl + "/" + pollId;
        }
    }

}

