using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;
using System.Threading;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class MessageListViewModel
    {
        public QueueInfo QueueInfo { get; set; }
        public int Count { get; set; }
        public UserSession UserSession { get; set; }
    }
}
