using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SFA.DAS.Tools.Servicebus.Support.Domain.Queue
{
    public class QueueMessage
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
        public Message OriginalMessage { get; set; }
        public string Queue { get; set; }
        public bool IsReadOnly { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
        [JsonProperty("originatingEndpoint")]
        public string OriginatingEndpoint { get; set; }
        [JsonProperty("processingEndpoint")]
        public string ProcessingEndpoint { get; set; }
        [JsonProperty("exception")]
        public string Exception { get; set; }
        [JsonProperty("exceptionType")]
        public string ExceptionType { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public IEnumerable<KeyValuePair<string, object>> UserProperties => OriginalMessage.UserProperties.OrderBy((x => x.Key));

    }
}
