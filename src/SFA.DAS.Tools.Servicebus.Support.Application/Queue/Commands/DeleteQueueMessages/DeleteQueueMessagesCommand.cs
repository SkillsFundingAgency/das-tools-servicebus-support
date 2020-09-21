using System.Collections.Generic;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage
{
    public class DeleteQueueMessagesCommand
    {
        public IEnumerable<string> Ids { get; set; }
    }
}
