using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages
{
    public class BulkCreateQueueMessagesCommandHandler : ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public BulkCreateQueueMessagesCommandHandler(ICosmosDbContext cosmosDbContext)
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
