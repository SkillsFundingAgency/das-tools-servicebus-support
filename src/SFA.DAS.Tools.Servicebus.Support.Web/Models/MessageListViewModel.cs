using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class MessageListViewModel
    {
        public QueueInfo QueueInfo { get; set; }
        public IEnumerable<QueueMessage> Messages { get; set; }
    }
}
