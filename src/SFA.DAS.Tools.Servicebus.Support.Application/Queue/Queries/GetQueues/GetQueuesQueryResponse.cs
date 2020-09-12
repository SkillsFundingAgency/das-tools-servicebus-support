using System;
using System.Collections.Generic;
using System.Text;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues
{
    public class GetQueuesQueryResponse
    {
        public IEnumerable<QueueInfo> Queues { get; set; }
    }
}
