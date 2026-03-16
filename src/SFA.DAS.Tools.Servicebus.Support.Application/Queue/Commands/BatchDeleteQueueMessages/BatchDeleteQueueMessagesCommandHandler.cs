using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using MoreLinq;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;

public class BatchDeleteQueueMessagesCommandHandler(
    ICosmosMessageDbContext cosmosDbContext,
    ServiceBusErrorManagementSettings serviceBusSettings,
    ILogger<BatchDeleteQueueMessagesCommandHandler> logger,
    IAuditService auditService)
    : ICommandHandler<BatchDeleteQueueMessagesCommand,
        BatchDeleteQueueMessagesCommandResponse>
{
    private readonly int _batchSize = serviceBusSettings.PeekMessageBatchSize;
    private readonly ILogger _logger = logger;

    public async Task<BatchDeleteQueueMessagesCommandResponse> Handle(BatchDeleteQueueMessagesCommand query)
    {
        if (query.Ids == null || !query.Ids.Any())
        {
            return new BatchDeleteQueueMessagesCommandResponse();
        }

        foreach (var batch in query.Ids.Batch(_batchSize))
        {
            using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                var ids = batch.ToList();
                await cosmosDbContext.DeleteQueueMessagesAsync(ids);

                ts.Complete();

                await auditService.WriteAudit(new MessageQueueDeleteAuditMessage(ids));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete messages");
            }
        }

        return new BatchDeleteQueueMessagesCommandResponse();
    }
}