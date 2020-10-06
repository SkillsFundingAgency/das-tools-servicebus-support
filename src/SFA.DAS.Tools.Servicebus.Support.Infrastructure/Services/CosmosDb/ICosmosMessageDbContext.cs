using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public interface ICosmosMessageDbContext
    {
        Task CreateQueueMessageAsync(QueueMessage msg);
        Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messages);
        Task DeleteQueueMessagesAsync(IEnumerable<string> ids);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId, SearchProperties searchProperties);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesByIdAsync(string userId, IEnumerable<string> ids);
        Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId);
        Task<int> GetMessageCountAsync(string userId, SearchProperties searchProperties = null);
        Task<bool> HasUserAnExistingSession(string userId);        
    }
}
