using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession
{
    public class GetUserSessionQueryHandler : IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public GetUserSessionQueryHandler(ICosmosDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetUserSessionQueryResponse> Handle(GetUserSessionQuery query)
        {
            var userHasExistingSession = await _cosmosDbContext.HasUserAnExistingSession(query.UserId);

            return new GetUserSessionQueryResponse()
            {
                UserHasExistingSession = userHasExistingSession
            };
        }
    }
}
