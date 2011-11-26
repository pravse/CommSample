using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WcfCommService
{
    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public string Id { get; set; }
        
        [DataMember]
        public bool Truthful { get; set; }

        [DataMember]
        public string Answer { get; set; }
    }

    public interface IServiceResponseStore {

        void Add(ServiceResponse NewResponse);

        ServiceResponse Get(string id);

        void Delete(string id);

        IEnumerable<ServiceResponse> All { get; }
    }

    public class ServiceResponseStore : IServiceResponseStore
    {
        Dictionary<string, ServiceResponse> responseDict;
        public ServiceResponseStore()
        {
            responseDict = new Dictionary<string, ServiceResponse>();
        }

        public IEnumerable<ServiceResponse> All { get { return responseDict.Values; } }

        public void Add(ServiceResponse NewResponse)
        {
            if (null != NewResponse)
            {
                responseDict.Add(NewResponse.Id, NewResponse);
            }
        }

        public ServiceResponse Get(string id)
        {
            if (responseDict.ContainsKey(id))
            {
                return responseDict[id];
            }
            else
            {
                return null;
            }
        }

        public void Delete(string id)
        {
            responseDict.Remove(id);
        }

    }
}
