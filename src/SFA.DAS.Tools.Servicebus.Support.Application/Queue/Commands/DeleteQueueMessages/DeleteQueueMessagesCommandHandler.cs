using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage
{
    public class DeleteQueueMessagesCommandHandler : ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>
    {
        private readonly ICosmosMessageDbContext _cosmosDbContext;

        public DeleteQueueMessagesCommandHandler(ICosmosMessageDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<DeleteQueueMessagesCommandResponse> Handle(DeleteQueueMessagesCommand query)
        {
            await _cosmosDbContext.DeleteQueueMessagesAsync(query.Ids);

            return new DeleteQueueMessagesCommandResponse();
        }
    }
}
