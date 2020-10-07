using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession
{
    public class GetUserSessionQueryHandler : IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse>
    {
        private readonly ICosmosUserSessionDbContext _cosmosDbContext;

        public GetUserSessionQueryHandler(ICosmosUserSessionDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetUserSessionQueryResponse> Handle(GetUserSessionQuery query)
        {
            var userSession = await _cosmosDbContext.GetUserSessionAsync(query.UserId);

            return new GetUserSessionQueryResponse()
            {
                UserSession = userSession
            };
        }
    }
}
