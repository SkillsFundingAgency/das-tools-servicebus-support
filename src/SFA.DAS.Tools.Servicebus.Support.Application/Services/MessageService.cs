using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public enum Transactional
    {
        Yes,
        No
    }

    public class MessageService : IMessageService
    {
        private readonly ILogger<MessageService> _logger;

        private readonly ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>
            _bulkCreateMessagesCommand;

        private readonly IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>
            _receiveQueueMessagesQuery;

        private readonly IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>
            _getQueueMessageCountQuery;

        private readonly IBatchGetMessageStrategy _batchMessageStrategy;

        private readonly IDictionary<Transactional, Func<string, int, Task<IList<QueueMessage>>>> _processor =
            new ConcurrentDictionary<Transactional, Func<string, int, Task<IList<QueueMessage>>>>();

        private readonly int _batchSize;
        private readonly ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> _sendMessagesCommand;

        private readonly ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>
            _deleteQueueMessageCommand;

        private readonly IBatchSendMessageStrategy _batchSendMessageStrategy;

        public MessageService(
            ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>
                bulkCreateMessagesCommand,
            IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse> receiveQueueMessagesQuery,
            IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse> getQueueMessageCountQuery,
            IBatchGetMessageStrategy batchMessageStrategy,
            IBatchSendMessageStrategy batchSendMessageStrategy,
            ILogger<MessageService> logger,
            int batchSize,
            ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> sendMessagesCommand,
            ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse> deleteQueueMessageCommand
        )
        {
            _bulkCreateMessagesCommand = bulkCreateMessagesCommand;
            _receiveQueueMessagesQuery = receiveQueueMessagesQuery;
            _batchMessageStrategy = batchMessageStrategy;
            _batchSendMessageStrategy = batchSendMessageStrategy;
            _getQueueMessageCountQuery = getQueueMessageCountQuery;
            _logger = logger;
            _batchSize = batchSize;
            _sendMessagesCommand = sendMessagesCommand;
            _deleteQueueMessageCommand = deleteQueueMessageCommand;
            _processor.Add(Transactional.Yes, ProcessMessagesInTransaction);
            _processor.Add(Transactional.No, ProcessMessages);
        }

        public async Task ProcessMessages(string queue, Transactional transaction = Transactional.Yes)
        {
            var count = await GetMessageCount(queue);

            await _batchMessageStrategy.Execute(queue, count, _batchSize,
                async (qty) => await _processor[transaction](queue, qty), async (message) => message);
        }

        private async Task<long> GetMessageCount(string queue)
        {
            return (await _getQueueMessageCountQuery.Handle(new GetQueueMessageCountQuery()
            {
                QueueName = queue
            })).Count;
        }

        private async Task<IList<QueueMessage>> ProcessMessagesInTransaction(string queue, int quantity)
        {
            IEnumerable<QueueMessage> messages = new List<QueueMessage>();

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    messages = await GetMessages(queue, quantity);
                    await AddMessagesToDatabase(messages);

                    ts.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to receive messages");
                }
            }

            return messages.ToList();
        }

        private async Task<IList<QueueMessage>> ProcessMessages(string queue, int quantity)
        {
            IEnumerable<QueueMessage> messages = new List<QueueMessage>();

            try
            {
                messages = await GetMessages(queue, quantity);
                await AddMessagesToDatabase(messages);

                return messages.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to receive messages");
            }

            return messages.ToList();
        }

        private async Task<IEnumerable<QueueMessage>> GetMessages(string queue, int messageQtyToGet)
        {
            var response = await _receiveQueueMessagesQuery.Handle(new ReceiveQueueMessagesQuery()
            {
                QueueName = queue,
                Quantity = messageQtyToGet
            });

            return response.Messages;
        }

        private async Task AddMessagesToDatabase(IEnumerable<QueueMessage> messages)
        {
            await _bulkCreateMessagesCommand.Handle(new BulkCreateQueueMessagesCommand()
            {
                Messages = messages
            });
        }

        public async Task AbortMessages(IEnumerable<QueueMessage> messages, string queue)
        {
            await SendMessageAndDeleteFromDb(messages, queue);
        }

        private async Task SendMessageAndDeleteFromDb(IEnumerable<QueueMessage> messages, string queue)
        {
            await _batchSendMessageStrategy.Execute(messages, _batchSize,
                async (messages) =>
                {
                    using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                    try
                    {
                        await _sendMessagesCommand.Handle(new SendMessagesCommand()
                        {
                            Messages = messages,
                            QueueName = queue
                        });

                        await _deleteQueueMessageCommand.Handle(new DeleteQueueMessagesCommand()
                        {
                            Ids = messages.Select(x => x.Id).ToList()
                        });

                        ts.Complete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send messages");
                    }
                });
        }

        public async Task ReplayMessages(IEnumerable<QueueMessage> messages, string queue)
        {
            await SendMessageAndDeleteFromDb(messages, queue);
        }

        public async Task DeleteMessages(IEnumerable<string> ids)
        {
            await _batchSendMessageStrategy.Execute(ids, _batchSize,
                async (messages) =>
                {
                    using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                    try
                    {
                        await _deleteQueueMessageCommand.Handle(new DeleteQueueMessagesCommand()
                        {
                            Ids = ids
                        });

                        ts.Complete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete messages");
                    }
                });
        }
    }
}
