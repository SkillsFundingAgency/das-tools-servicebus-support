using System.Collections.Generic;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages
{
    public class BulkCreateQueueMessagesCommand
    {
        public IEnumerable<QueueMessage> Messages { get; set; }
    }
}
