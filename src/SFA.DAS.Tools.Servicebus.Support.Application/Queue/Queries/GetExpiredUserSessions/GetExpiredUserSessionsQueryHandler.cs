using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions
{
    public class GetExpiredUserSessionsQueryHandler : IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse>
    {
        private readonly ICosmosUserSessionDbContext _cosmosDbContext;

        public GetExpiredUserSessionsQueryHandler(ICosmosUserSessionDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetExpiredUserSessionsQueryResponse> Handle(GetExpiredUserSessionsQuery query)
        {
            var expiredUserSessions = await _cosmosDbContext.GetExpiredUserSessionsAsync();

            return new GetExpiredUserSessionsQueryResponse
            {
                ExpiredUserSessions = expiredUserSessions
            };
        }
    }
}
