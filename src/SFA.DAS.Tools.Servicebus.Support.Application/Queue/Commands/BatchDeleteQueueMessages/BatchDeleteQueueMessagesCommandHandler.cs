using System;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages
{
    public class BatchDeleteQueueMessagesCommandHandler : ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse>
    {
        private readonly ICosmosMessageDbContext _cosmosDbContext;
        private readonly IBatchSendMessageStrategy _batchSendMessageStrategy;
        private readonly int _batchSize;
        private readonly ILogger _logger;

        public BatchDeleteQueueMessagesCommandHandler(
            ICosmosMessageDbContext cosmosDbContext,
            IBatchSendMessageStrategy batchSendMessageStrategy,
            int batchSize,
            ILogger<BatchDeleteQueueMessagesCommandHandler> logger)
        {
            _cosmosDbContext = cosmosDbContext;
            _batchSize = batchSize;
            _batchSendMessageStrategy = batchSendMessageStrategy;
            _logger = logger;
        }

        public async Task<BatchDeleteQueueMessagesCommandResponse> Handle(BatchDeleteQueueMessagesCommand query)
        {
            await _batchSendMessageStrategy.Execute(query.Ids, _batchSize,
                async (messages) =>
                {
                    using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                    try
                    {
                        await _cosmosDbContext.DeleteQueueMessagesAsync(query.Ids);

                        ts.Complete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to delete messages", ex);
                    }
                });

            return new BatchDeleteQueueMessagesCommandResponse();
        }
    }
}
