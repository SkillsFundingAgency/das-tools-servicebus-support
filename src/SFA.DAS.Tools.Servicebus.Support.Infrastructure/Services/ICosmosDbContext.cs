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
        Task DeleteQueueMessagesAsync(IEnumerable<string> ids);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId, SearchProperties searchProperties);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesByIdAsync(string userId, IEnumerable<string> ids);
        Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId);
        Task<int> GetMessageCountAsync(string userId, SearchProperties searchProperties = null);
        Task<bool> HasUserAnExistingSession(string userId);
        Task<UserSession> CreateUserSessionAsync(UserSession userSession);
        Task<UserSession> GetUserSessionAsync(string userId);
        Task DeleteUserSessionAsync(string id, string userId);
    }
}
