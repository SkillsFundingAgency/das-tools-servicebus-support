﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

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
        private readonly ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse> _bulkCreateMessagesCommand;
        private readonly IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>  _receiveQueueMessagesQuery;
        private readonly IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse> _getQueueMessageCountQuery;
        private readonly IBatchMessageStrategy _batchMessageStrategy;
        private readonly IDictionary<Transactional, Func<string, int, Task<IList<QueueMessage>>>> _processor = new ConcurrentDictionary<Transactional, Func<string, int, Task<IList<QueueMessage>>>>();
        private readonly int _batchSize;

        public MessageService(
            ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse> bulkCreateMessagesCommand,
            IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse> receiveQueueMessagesQuery,
            IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse> getQueueMessageCountQuery,
            IBatchMessageStrategy batchMessageStrategy,
            ILogger<MessageService> logger,
            int batchSize)
        {
            _bulkCreateMessagesCommand = bulkCreateMessagesCommand;
            _receiveQueueMessagesQuery = receiveQueueMessagesQuery;
            _batchMessageStrategy = batchMessageStrategy;
            _getQueueMessageCountQuery = getQueueMessageCountQuery;
            _logger = logger;
            _batchSize = batchSize;

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
                    _logger.LogError("Failed to receive messages", ex);
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
                _logger.LogError("Failed to receive messages", ex);
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
    }
}
