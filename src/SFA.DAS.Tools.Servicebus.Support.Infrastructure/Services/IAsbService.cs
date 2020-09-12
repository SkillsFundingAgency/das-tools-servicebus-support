using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface IAsbService
    {
        Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync();
        Task<QueueInfo> GetQueueDetailsAsync(string name);
        Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty);
        Task<ReceiveMessagesResponse> ReceiveMessagesAsync(string queueName, int qty);
        Task SendMessageToErrorQueueAsync(QueueMessage msg);
        Task SendMessageToProcessingQueueAsync(QueueMessage msg);
    }
}
