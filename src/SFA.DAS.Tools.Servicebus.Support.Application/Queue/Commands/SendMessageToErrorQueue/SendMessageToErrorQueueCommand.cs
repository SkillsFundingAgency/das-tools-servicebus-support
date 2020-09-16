using System.Collections.Generic;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessageToErrorQueue
{
    public class SendMessageToErrorQueueCommand
    {
        public QueueMessage Message { get; set; }
    }
}
