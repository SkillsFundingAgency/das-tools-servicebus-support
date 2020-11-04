using Microsoft.Extensions.Logging;
using MoreLinq;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages
{
    public class BatchDeleteQueueMessagesCommandHandler : ICommandHandler<BatchDeleteQueueMessagesCommand,
        BatchDeleteQueueMessagesCommandResponse>
    {
        private readonly ICosmosMessageDbContext _cosmosDbContext;
        private readonly int _batchSize;
        private readonly ILogger _logger;

        public BatchDeleteQueueMessagesCommandHandler(
            ICosmosMessageDbContext cosmosDbContext,
            ServiceBusErrorManagementSettings serviceBusSettings,
            ILogger<BatchDeleteQueueMessagesCommandHandler > logger)
        {
            _cosmosDbContext = cosmosDbContext;
            _batchSize = serviceBusSettings.PeekMessageBatchSize;
            _logger = logger;
        }

        public async Task<BatchDeleteQueueMessagesCommandResponse> Handle(BatchDeleteQueueMessagesCommand query)
        {
            foreach (var batch in query.Ids.Batch(_batchSize))
            {
                using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                try
                {
                    await _cosmosDbContext.DeleteQueueMessagesAsync(query.Ids);

                    ts.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete messages");
                }
            }

            return new BatchDeleteQueueMessagesCommandResponse();
        }
    }
}
