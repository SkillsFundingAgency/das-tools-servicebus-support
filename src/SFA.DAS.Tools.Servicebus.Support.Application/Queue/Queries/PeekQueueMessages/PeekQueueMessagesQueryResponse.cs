using System;
using System.Collections.Generic;
using System.Text;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages
{
    public class PeekQueueMessagesQueryResponse
    {
        public IEnumerable<QueueMessage> Messages { get; set; }
    }
}
