using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession
{
    public class DeleteUserSessionCommandHandler : ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>
    {
        private readonly ICosmosUserSessionDbContext _cosmosDbContext;

        public DeleteUserSessionCommandHandler(ICosmosUserSessionDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<DeleteUserSessionCommandResponse> Handle(DeleteUserSessionCommand query)
        {
            await _cosmosDbContext.DeleteUserSessionAsync(query.Id, query.UserId);

            return new DeleteUserSessionCommandResponse();
        }
    }
}
