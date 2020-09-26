using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.CreateUserSession
{
    public class CreateUserSessionCommandHandler : ICommandHandler<CreateUserSessionCommand, CreateUserSessionCommandResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public CreateUserSessionCommandHandler(ICosmosDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<CreateUserSessionCommandResponse> Handle(CreateUserSessionCommand query)
        {
            var userSession = await _cosmosDbContext.CreateUserSessionAsync(query.UserSession);

            return new CreateUserSessionCommandResponse
            {
                UserSession = userSession
            };
        }
    }
}
