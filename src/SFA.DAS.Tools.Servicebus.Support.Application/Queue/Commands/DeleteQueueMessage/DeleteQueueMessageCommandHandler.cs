using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage
{
    public class DeleteQueueMessageCommandHandler : ICommandHandler<DeleteQueueMessageCommand, DeleteQueueMessageCommandResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public DeleteQueueMessageCommandHandler(ICosmosDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<DeleteQueueMessageCommandResponse> Handle(DeleteQueueMessageCommand query)
        {
            await _cosmosDbContext.DeleteQueueMessageAsync(query.Message);

            return new DeleteQueueMessageCommandResponse();
        }
    }
}
