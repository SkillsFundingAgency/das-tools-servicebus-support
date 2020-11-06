using Newtonsoft.Json;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Domain
{
    public class GetMessagesRequest
    {        
        [JsonProperty("queue")]
        public string Queue { get; set; }
        [JsonProperty("qty")]
        public int Qty { get; set; }
    }
}
