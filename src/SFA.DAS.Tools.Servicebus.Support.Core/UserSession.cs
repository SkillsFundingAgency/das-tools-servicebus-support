using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace SFA.DAS.Tools.Servicebus.Support.Domain
{
    public class UserSession
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("userId")]
        [Required]
        public string UserId { get; set; }
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; } = "session";
        [JsonProperty("expiryDateUtc")]
        public DateTime ExpiryDateUtc { get; set; }
        [JsonProperty("queue")]
        public string Queue { get; set; }
    }
}
