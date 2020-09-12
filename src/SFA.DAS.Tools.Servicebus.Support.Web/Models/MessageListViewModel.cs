using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System.Collections.Generic;
using System.Threading;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class MessageListViewModel
    {
        public QueueInfo QueueInfo { get; set; }
        public int Count { get; set; }
    }
}
