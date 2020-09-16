using System;
using System.Collections.Generic;
using System.Text;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages
{
    public class GetMessagesQueryResponse
    {
        public IEnumerable<QueueMessage> Messages { get; set; }
        public int Count { get; set; }

        public int UnfilteredCount { get; set; }
    }
}
