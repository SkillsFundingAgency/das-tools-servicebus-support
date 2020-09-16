using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface ICosmosDbContext
    {
        Task CreateQueueMessageAsync(QueueMessage msg);
        Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messages);
        Task DeleteQueueMessageAsync(QueueMessage msg);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId, SearchProperties searchProperties);
        Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId);
        Task<int> GetMessageCountAsync(string userId, SearchProperties searchProperties = null);
        Task<bool> HasUserAnExistingSession(string userId);
    }
}
