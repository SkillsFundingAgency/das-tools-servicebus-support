using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Exceptions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public class RetrieveMessagesService : IRetrieveMessagesService
    {
        private readonly IBatchGetMessageStrategy _batchMessageStrategy;
        private readonly ILogger<RetrieveMessagesService> _logger;
        private readonly IMessageReceiverFactory _messageReceiverFactory;
        private readonly IUserService _userService;
        private volatile IMessageReceiver _messageReceiver;
        private readonly object _padlock = new object();
        private readonly ICosmosMessageDbContext _cosmosDbContext;
        private readonly int _batchSize;

        public RetrieveMessagesService(
            ServiceBusErrorManagementSettings serviceBusSettings,
            ILogger<RetrieveMessagesService> logger,
            IBatchGetMessageStrategy batchMessageStrategy,
            IUserService userService,
            ICosmosMessageDbContext cosmosDbContext,
            IMessageReceiverFactory messageReceiverFactory
        )
        {
            _batchMessageStrategy = batchMessageStrategy;
            _logger = logger;
            _batchSize = serviceBusSettings.PeekMessageBatchSize;
            _userService = userService;
            _cosmosDbContext = cosmosDbContext;
            _messageReceiverFactory = messageReceiverFactory;
        }

        public async Task GetMessages(string queueName, long count, int getQty)
        {
            count = count > getQty ? getQty : count;

            CreateMessageReceiver(queueName, 250);

            await _batchMessageStrategy.Execute(queueName, count, _batchSize,
                async (qty) => await ProcessMessagesInTransaction(queueName, qty), async (message) => message);

            await _messageReceiver.CloseAsync();
        }

        private async Task<IList<QueueMessage>> ProcessMessagesInTransaction(string queueName, int quantity)
        {
            IEnumerable<QueueMessage> formattedMessages = new List<QueueMessage>();
            var messages = await _messageReceiver.ReceiveAsync(quantity, TimeSpan.FromSeconds(60));

            try
            {
                if (messages == null)
                {
                    return null;
                }

                formattedMessages = messages.Select(message => message.Convert(_userService.GetUserId(), queueName));

                await AddMessagesToDatabase(formattedMessages);
                await _messageReceiver.CompleteAsync(messages.Select(message => message.GetLockToken()));
            }
            catch (CosmosBatchInsertException ex)
            {
                await HandleBatchCreationFailure(formattedMessages, ex.StatusCode);

                _logger.LogError("Failed to create messages with error: {0}, exception: {1}", ex.StatusCode.ToString(),
                    ex.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to receive messages: {0}", ex.ToString());

                foreach (var message in formattedMessages)
                {
                    await _messageReceiver.AbandonAsync(message.OriginalMessage.GetLockToken());
                }
            }

            return formattedMessages.ToList();
        }

        private async Task HandleBatchCreationFailure(IEnumerable<QueueMessage> formattedMessages, HttpStatusCode statusCode)
        {
            try
            {
                await _messageReceiver.RenewLockAsync(formattedMessages.Select(message => message.OriginalMessage.GetLockToken()));

                foreach (var message in formattedMessages)
                {
                    if (statusCode != HttpStatusCode.Conflict)
                    {
                        if ((await _cosmosDbContext.MessageExistsAsync(_userService.GetUserId(), message.Id)))
                        {
                            await _messageReceiver.DeadLetterAsync(message.OriginalMessage.GetLockToken(),
                                "Conflict in CosmosDB", "Duplicate message");
                            continue;
                        }
                    }

                    await _messageReceiver.AbandonAsync(message.OriginalMessage.GetLockToken());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not renew locks");
            }
        }

        private void CreateMessageReceiver(string queueName, int? prefetch = null)
        {
            if (_messageReceiver != null) return;

            lock (_padlock)
            {
                if (_messageReceiver != null) return;

                _messageReceiver = _messageReceiverFactory.Create(queueName);

                if (prefetch.HasValue)
                {
                    _messageReceiver.PrefetchCount = prefetch.Value;
                }
            }
        }

        private async Task AddMessagesToDatabase(IEnumerable<QueueMessage> messages)
        {
            _logger.LogInformation($"# of messages bulk created: {messages.Count()}");
            await _cosmosDbContext.BulkCreateQueueMessagesAsync(messages);
        }
    }
}
