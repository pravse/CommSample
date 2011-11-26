using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Diagnostics;

/***
 * This service exposes the following capabilities:
 *   1) send email to a person with a structured message
 *   2) read the reply, parse it, and present it back to the user
 *   3) schedule a meeting with a person (create a Doodle poll, send a structured message, read mail waiting for a reply)
 *   4) call a person with a structured message and get a reply
 * 
 * 
 * ***/

namespace WcfCommService
{

    // Start the service and browse to http://<machine_name>:<port>/Service1/help to view the service's generated help page
    // NOTE: By default, a new instance of the service is created for each call; change the InstanceContextMode to Single if you want
    // a single instance of the service to process all calls.	
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    // NOTE: If the service is renamed, remember to update the global.asax.cs file
    public class CommService
    {
        CommLogic commLogic;
        IServiceRequestStore requestStore; 
        IServiceResponseStore responseStore; 

        public CommService()
        {
            requestStore = new ServiceRequestStore();
            responseStore = new ServiceResponseStore();
            commLogic = new CommLogic(Global.GmailClient, Global.DoodleClient, Global.TwilioClient, this.NewResponseCallback);
        }

        [OperationContract]
        [WebGet(UriTemplate = "request")]
        public IEnumerable<ServiceRequest> All()
        {
            return requestStore.All;
        }

        [OperationContract]
        [WebInvoke(UriTemplate = "request", Method = "POST", 
                    RequestFormat=WebMessageFormat.Json, 
                    ResponseFormat=WebMessageFormat.Json)]
        public RequestHandle CreateRequest(ServiceRequest instance)
        {
            // process the new request
            RequestHandle reqHandle = commLogic.HandleNewRequest(instance);

            if (null != reqHandle)
            {
                // add it to the store
                instance.Handle = reqHandle;
                requestStore.Add(reqHandle.Id, instance);
            }

            return reqHandle; 
        }

        [OperationContract]
        [WebGet(UriTemplate = "request/{id}", 
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public ServiceRequest GetRequest(string id)
        {
	    return requestStore.Get(id);
        }

        [OperationContract]
        [WebInvoke(UriTemplate = "request/{id}", Method = "POST",
                                RequestFormat = WebMessageFormat.Json,
                                ResponseFormat = WebMessageFormat.Json,
                                BodyStyle=WebMessageBodyStyle.WrappedRequest)]
        public ServiceRequest UpdateOrDelete(string id, ServiceRequest instance, string method)
        {
              if ((null != method))
                {
                    // implement update as delete + insert
                    requestStore.Delete(id);
                }
              if ((null != method) && method.Equals("update") && (null != instance))
                {
                    requestStore.Add(id, instance);
                    return instance;
                }
              return null;
        }

        public void NewResponseCallback(ServiceResponse response)
        {
            // add it to the store
            responseStore.Add(response);
        }

        [OperationContract]
        [WebGet(UriTemplate = "/response/{id}",
                ResponseFormat = WebMessageFormat.Json,
                BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        public ServiceResponse GetResponse(string id)
        {
            ServiceResponse response = responseStore.Get(id);

            if (null == response)
            {
                // check the channel --- if the response is via the Doodle channel,
                // we need to invoke the Doodle API to get the response
                ServiceRequest request = requestStore.Get(id);
                if ((null != request) && (ChannelChoice.EMAIL_DOODLE == request.Channel))
                {
                    // this calls the Doodle API, and if there is a response, it invokes the calback which inserts 
                    // the response into the response store.
                    RequestHandle handle = request.Handle;
                    Debug.Assert(null != handle);
                    commLogic.CheckResponseImmediate(handle.Id, handle.Channel, handle.Metadata, NewResponseCallback);
                    response = responseStore.Get(id);
                }
                else // assume (for the purpose of debugging that (ChannelChoice.EMAIL_DOODLE == request.Channel)
                {
                    // this calls the Doodle API, and if there is a response, it invokes the calback which inserts 
                    // the response into the response store.
                    commLogic.CheckResponseImmediate(id, ChannelChoice.EMAIL_DOODLE, null, NewResponseCallback);
                    response = responseStore.Get(id);
                }
            }

            return response;
        }


        [OperationContract]
        [WebGet(UriTemplate = "/TwiML",
                ResponseFormat = WebMessageFormat.Xml)]
        public string GetTwiMLResponse()
        {
    	    // get the request parameters

	       // provide the TwiML response
            return null;
	    }

    
    }
}
