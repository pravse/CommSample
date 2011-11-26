using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WcfCommService
{
    public enum ChannelChoice
    {
        EMAIL_EMAIL = 1,
        EMAIL_DOODLE = 2,
        TWILIO = 3
    }

    // immediate response to the ServiceRequest
    public class RequestHandle
    {
        public string Id;
        public ChannelChoice Channel;
        public string Metadata;
    }

    [DataContract]
    public class ServiceRequest
    {
        [DataMember]
        public string Recipient { get; set; }

        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string Question { get; set; }

        [DataMember]
        public ChannelChoice Channel { get; set; }

        [DataMember]
        // Metadata that gets added onto the request as it is processed.
        // TODO: This should idealluy be stored elsewhere, but for simplicity in this sample, leaving it here.
        public RequestHandle Handle { get; set; }
    }

    public interface IServiceRequestStore {

        void Add(string id, ServiceRequest newRequest);

        ServiceRequest Get(string id);

        void Delete(string id);

        IEnumerable<ServiceRequest> All { get; }

    }

    public class ServiceRequestStore : IServiceRequestStore
    {
        Dictionary<string, ServiceRequest> requestDict;
        public ServiceRequestStore()
        {
            requestDict = new Dictionary<string, ServiceRequest>();
        }

        public IEnumerable<ServiceRequest> All { get { return requestDict.Values; } }

        public void Add(string requestId, ServiceRequest newRequest)
        {
            if ((null != newRequest) && (null != requestId))
            {
                requestDict.Add(requestId, newRequest);
            }
        }

        public ServiceRequest Get(string id)
        {
            return requestDict[id];
        }

        public void Delete(string id)
        {
            requestDict.Remove(id);
        }

    }
}
