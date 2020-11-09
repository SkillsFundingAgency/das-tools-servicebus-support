using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class ReleaseSelectedMessages
    {
        public List<string> Ids { get; set; }
        public string QueueName { get; set; }
    }
}
