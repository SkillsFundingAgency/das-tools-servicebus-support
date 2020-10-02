using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession
{
    public class UpsertUserSessionCommandHandler : ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse>
    {
        private readonly ICosmosUserSessionDbContext _cosmosDbContext;

        public UpsertUserSessionCommandHandler(ICosmosUserSessionDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<UpsertUserSessionCommandResponse> Handle(UpsertUserSessionCommand query)
        {
            var userSession = await _cosmosDbContext.UpsertUserSessionAsync(query.UserSession);

            return new UpsertUserSessionCommandResponse
            {
                UserSession = userSession
            };
        }
    }
}
