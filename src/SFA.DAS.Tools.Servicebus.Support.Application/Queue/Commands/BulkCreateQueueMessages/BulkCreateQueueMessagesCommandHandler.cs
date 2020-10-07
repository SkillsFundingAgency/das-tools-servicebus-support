using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages
{
    public class BulkCreateQueueMessagesCommandHandler : ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>
    {
        private readonly ICosmosMessageDbContext _cosmosDbContext;

        public BulkCreateQueueMessagesCommandHandler(ICosmosMessageDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<BulkCreateQueueMessagesCommandResponse> Handle(BulkCreateQueueMessagesCommand query)
        {
            await _cosmosDbContext.BulkCreateQueueMessagesAsync(query.Messages);

            return new BulkCreateQueueMessagesCommandResponse();
        }
    }
}
