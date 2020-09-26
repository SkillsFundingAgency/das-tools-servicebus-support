using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Domain
{
    public class UserSession
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; } = "session";
        public DateTime ExpiryDateUtc { get; set; }
    }
}
