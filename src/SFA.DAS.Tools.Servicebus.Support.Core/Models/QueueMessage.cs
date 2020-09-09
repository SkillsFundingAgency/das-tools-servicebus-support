using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Core.Models
{
    public class QueueMessage
    {
        public string id { get; set; }
        public string userId { get; set; }
        public Message OriginalMessage { get; set; }
        public string Queue { get; set; }
        public bool IsReadOnly { get; set; }
        public IEnumerable<KeyValuePair<string, object>> NServiceBusProperties => OriginalMessage.UserProperties.Where(x => x.Key.ToLower().Contains("nservicebus"));

        public IEnumerable<KeyValuePair<string, object>> ServiceBusProperties => OriginalMessage.UserProperties.Where(x => !x.Key.ToLower().Contains("nservicebus"));

        public string Body => Encoding.UTF8.GetString(OriginalMessage.Body);
    }
}
