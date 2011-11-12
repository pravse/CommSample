using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WcfRestServiceSample
{
    // TODO: Edit the SampleItem class
    [DataContract]
    public class SampleItem
    {
        [DataMember]
        public int Id { get; set; }
        
        [DataMember]
        public string StringValue { get; set; }
    }

    public class SampleItemStore
    {
        Dictionary<int, SampleItem> itemList;
        public SampleItemStore()
        {
            itemList = new Dictionary<int, SampleItem>();
            itemList.Add(1, new SampleItem { Id = 1, StringValue = "one" }); 
        }

        public IDictionary<int, SampleItem> ItemList { get { return itemList; } }

        public void AddItem(SampleItem NewItem)
        {
            if (null != NewItem)
            {
                itemList.Add(NewItem.Id, NewItem);
            }
        }

        public SampleItem GetItem(int id)
        {
            return itemList[id];
        }

        public void DeleteItem(int id)
        {
            itemList.Remove(id);
        }
    }
}
