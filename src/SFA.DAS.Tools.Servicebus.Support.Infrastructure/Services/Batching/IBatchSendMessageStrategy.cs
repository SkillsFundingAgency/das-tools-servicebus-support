using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching
{
    public interface IBatchSendMessageStrategy
    {
        Task Execute(IEnumerable<QueueMessage> messages, Func<IEnumerable<QueueMessage>, Task> sendMessages);
    }
}
