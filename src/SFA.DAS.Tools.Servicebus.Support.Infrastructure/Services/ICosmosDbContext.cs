using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface ICosmosDbContext
    {
        Task CreateQueueMessageAsync(QueueMessage msg);
        Task<BulkOperationResponse<QueueMessage>> BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messsages);
        Task DeleteQueueMessageAsync(QueueMessage msg);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId);
        Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId);
        Task<int> GetUserMessageCountAsync(string userId);
        Task<bool> HasUserAnExistingSession(string userId);
    }
}
