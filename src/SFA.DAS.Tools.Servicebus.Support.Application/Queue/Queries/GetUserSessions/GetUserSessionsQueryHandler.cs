using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSessions
{
    public class GetUserSessionsQueryHandler : IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse>
    {
        private readonly ICosmosUserSessionDbContext _cosmosDbContext;

        public GetUserSessionsQueryHandler(ICosmosUserSessionDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetUserSessionsQueryResponse> Handle(GetUserSessionsQuery query)
        {
            var userSessions = await _cosmosDbContext.GetUserSessionsAsync();

            return new GetUserSessionsQueryResponse()
            {
                UserSessions = userSessions
            };
        }
    }
}
