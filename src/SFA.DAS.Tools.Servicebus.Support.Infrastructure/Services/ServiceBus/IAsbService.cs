using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus
{
    public interface IAsbService
    {
        Task<IEnumerable<QueueInfo>> GetMessageQueuesAsync(int skipCount = 0, int takeCount = 100);
        Task<QueueInfo> GetQueueDetailsAsync(string name);
        Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty);
        Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty);
        Task SendMessagesAsync(IEnumerable<QueueMessage> messages, string queueName);        
        Task<long> GetQueueMessageCountAsync(string queueName);
    }
}
