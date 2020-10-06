using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public interface ICosmosUserSessionDbContext
    {
        Task<UserSession> UpsertUserSessionAsync(UserSession userSession);
        Task<UserSession> GetUserSessionAsync(string userId);
        Task DeleteUserSessionAsync(string id, string userId);
        Task<IEnumerable<UserSession>> GetExpiredUserSessions();
    }
}