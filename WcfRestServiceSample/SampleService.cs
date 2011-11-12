using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;

namespace WcfRestServiceSample
{
    // Start the service and browse to http://<machine_name>:<port>/Service1/help to view the service's generated help page
    // NOTE: By default, a new instance of the service is created for each call; change the InstanceContextMode to Single if you want
    // a single instance of the service to process all calls.	
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    // NOTE: If the service is renamed, remember to update the global.asax.cs file
    public class SampleService
    {
        SampleItemStore itemStore = new SampleItemStore();
        // TODO: Implement the collection resource that will contain the SampleItem instances

        [OperationContract]
        [WebGet(UriTemplate = "")]
        public IDictionary<int, SampleItem> GetCollection()
        {
            // TODO: Replace the current implementation to return a collection of SampleItem instances
            return itemStore.ItemList;
        }

        [OperationContract]
        [WebInvoke(UriTemplate = "", Method = "POST", 
                    RequestFormat=WebMessageFormat.Json, 
                    ResponseFormat=WebMessageFormat.Json)]
        public SampleItem Create(SampleItem instance)
        {
            itemStore.AddItem(instance);
            return instance;
        }

        [OperationContract]
        [WebGet(UriTemplate = "{id}")]
        public SampleItem Get(string id)
        {
            int IntId;
            if (Int32.TryParse(id, out IntId))
            {
                return itemStore.GetItem(IntId);
            }
            else return null;
            // throw new NotImplementedException();
        }

        [OperationContract]
        [WebInvoke(UriTemplate = "{id}", Method = "POST",
                                RequestFormat = WebMessageFormat.Json,
                                ResponseFormat = WebMessageFormat.Json,
                                BodyStyle=WebMessageBodyStyle.WrappedRequest)]
        public SampleItem UpdateOrDelete(string id, SampleItem instance, string method)
        {
            int IntId;
            if (Int32.TryParse(id, out IntId))
            {
                if ((null != method))
                {
                    // implement update as delete + insert
                    itemStore.DeleteItem(IntId);
                }
                if ((null != method) && method.Equals("update") && (null != instance))
                {
                    itemStore.AddItem(instance);
                    return instance;
                }
            }
            return null;
            // TODO: Update the given instance of SampleItem in the collection
        }
    }
}
