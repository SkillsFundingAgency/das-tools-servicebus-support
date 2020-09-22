using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById
{
    public class GetMessagesByIdQueryResponse
    {
        public IEnumerable<QueueMessage> Messages { get; set; }
    }
}
