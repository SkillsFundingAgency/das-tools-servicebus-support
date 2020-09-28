using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class MessageDetailViewModel
    {
        public string Queue { get; set; }
        public string Body { get; set; }
        public IEnumerable<KeyValuePair<string, string>> UserProperties { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Properties { get; set; }
    }
}
