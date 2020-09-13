using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface IAsbService
    {
        Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync();
        Task<QueueInfo> GetQueueDetailsAsync(string name);
        Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty);
        Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty);
        Task SendMessageToErrorQueueAsync(QueueMessage msg);
        Task SendMessageToProcessingQueueAsync(QueueMessage msg);
    }
}
