using System;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface IAsbService
    {
        Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync();
        Task<QueueInfo> GetQueueDetailsAsync(string name);
        Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty);
        Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty);
        Task SendMessagesAsync(IEnumerable<QueueMessage> messages, string queueName);        
        Task<long> GetQueueMessageCountAsync(string queueName);
    }
}
