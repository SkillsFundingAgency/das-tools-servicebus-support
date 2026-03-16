using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services;

public class MessageService(
    IBatchSendMessageStrategy batchSendMessageStrategy,
    ILogger<MessageService> logger,
    ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> sendMessagesCommand,
    ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse> deleteQueueMessageCommand,
    IAuditService auditService)
    : IMessageService
{
    public async Task ReplayMessages(IEnumerable<QueueMessage> messages, string queue) => await SendMessageAndDeleteFromDb(messages, queue,true);
        
    public async Task AbortMessages(IEnumerable<QueueMessage> messages, string queue) => await SendMessageAndDeleteFromDb(messages, queue);

    private async Task SendMessageAndDeleteFromDb(IEnumerable<QueueMessage> messages, string queue, bool createAudit = false)
    {
        await batchSendMessageStrategy.Execute(messages,
            async (messages) =>
            {
                using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                try
                {
                    await sendMessagesCommand.Handle(new SendMessagesCommand()
                    {
                        Messages = messages,
                        QueueName = queue
                    });

                    await deleteQueueMessageCommand.Handle(new DeleteQueueMessagesCommand()
                    {
                        Ids = messages.Select(x => x.Id).ToList()
                    });

                    ts.Complete();

                    if (createAudit)
                    {                               
                        foreach (var message in messages)
                        {
                            await auditService.WriteAudit(new MessageQueueReplayAuditMessage(message));                                
                        }
                    }                        
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send messages");
                    throw;
                }
            });
    }
}