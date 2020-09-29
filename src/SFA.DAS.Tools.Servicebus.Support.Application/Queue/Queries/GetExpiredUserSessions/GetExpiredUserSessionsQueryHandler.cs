using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions
{
    public class GetExpiredUserSessionsQueryHandler : IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public GetExpiredUserSessionsQueryHandler(ICosmosDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetExpiredUserSessionsQueryResponse> Handle(GetExpiredUserSessionsQuery query)
        {
            var expiredUserSessions = await _cosmosDbContext.GetExpiredUserSessions();

            return new GetExpiredUserSessionsQueryResponse
            {
                ExpiredUserSessions = expiredUserSessions    
            };
        }
    }
}
