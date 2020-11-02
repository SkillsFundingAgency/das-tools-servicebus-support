using Microsoft.Extensions.Logging;
using MoreLinq;
using SFA.DAS.Tools.Servicebus.Support.Audit;
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
        private readonly IAuditService _auditService;

        public BatchDeleteQueueMessagesCommandHandler(
            ICosmosMessageDbContext cosmosDbContext,
            int batchSize,
            ILogger<BatchDeleteQueueMessagesCommandHandler > logger,
            IAuditService auditService)
        {
            _cosmosDbContext = cosmosDbContext;
            _batchSize = batchSize;
            _logger = logger;
            _auditService = auditService;
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

                    await _auditService.WriteAudit(new MessageQueueDeleteAuditMessage(query.Ids));
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
