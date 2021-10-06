using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus
{
    public class AsbService : IAsbService
    {
        private readonly ILogger<AsbService> _logger;
        private readonly TokenProvider _tokenProvider;
        private readonly ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private readonly ManagementClient _managementClient;
        private readonly IUserService _userService;
        private volatile IMessageReceiver _messageReceiver;
        private readonly object _padlock = new object();
        private readonly IServiceBusPolicies _policies;

        public AsbService(IUserService userService,
            ServiceBusErrorManagementSettings serviceBusSettings,
            ILogger<AsbService> logger,
            IServiceBusPolicies serviceBusPolicies)
        {
            _logger = logger ?? throw new Exception("logger is null");
            _userService = userService;
            _policies = serviceBusPolicies;

            _sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusSettings.ServiceBusConnectionString);
            _tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            _managementClient = _sbConnectionStringBuilder.HasSasKey()
                ? new ManagementClient(_sbConnectionStringBuilder) : new ManagementClient(_sbConnectionStringBuilder, _tokenProvider);
        }

        public async Task<IEnumerable<QueueInfo>> GetMessageQueuesAsync(int skipCount = 0, int takeCount = 100)
        {
            var queues = await _policies.ResiliencePolicy.ExecuteAsync(async token =>
            {
                return await _managementClient.GetQueuesRuntimeInfoAsync(takeCount, skipCount, cancellationToken: token).ConfigureAwait(false);


            }, new CancellationToken());

            return queues?.Select(queue => new QueueInfo()
            {
                Name = queue.Path,
                MessageCount = queue.MessageCountDetails.ActiveMessageCount
            }).ToList();
        }

        public async Task<QueueInfo> GetQueueDetailsAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new QueueInfo();
            }

            var queueInfo = new QueueInfo();

            await _policies.ResiliencePolicy.ExecuteAsync(async token =>
            {
                var queue = await _managementClient.GetQueueRuntimeInfoAsync(name, token).ConfigureAwait(false);
                queueInfo = new QueueInfo()
                {
                    Name = queue.Path,
                    MessageCount = queue.MessageCountDetails.ActiveMessageCount
                };
            }, new CancellationToken());

            return queueInfo;
        }

        public async Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty)
        {
            var messageReceiver = CreateMessageReceiver(queueName);
            var messages = await messageReceiver.PeekAsync(qty);
            var formattedMessages = new List<QueueMessage>(messages.Count);
            formattedMessages.AddRange(messages.Select(message => message.Convert(_userService.GetUserId(), queueName)));

            await messageReceiver.CloseAsync();

            return formattedMessages;
        }

        public async Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty)
        {
            var messageReceiver = CreateMessageReceiver(queueName, 250);
            var messages = await messageReceiver.ReceiveAsync(qty, TimeSpan.FromSeconds(60));
            var formattedMessages = new List<QueueMessage>();

            if (messages == null) return new List<QueueMessage>();

            foreach (var message in messages)
            {
                await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
                formattedMessages.Add(message.Convert(_userService.GetUserId(), queueName));
            }

            return formattedMessages;
        }

        public async Task<long> GetQueueMessageCountAsync(string queueName) => (await GetQueueDetailsAsync(queueName)).MessageCount;

        public async Task SendMessagesAsync(IEnumerable<QueueMessage> messages, string queueName)
        {
            if (!messages.Any())
            {
                return;
            }

            var messageSender = CreateMessageSender(queueName);

            await _policies.ResiliencePolicy.ExecuteAsync(async token =>
            {
                var originalMessages = messages.Select(m => m.OriginalMessage).ToList();
                await messageSender.SendAsync(originalMessages);
            }, new CancellationToken());
        }

        private MessageSender CreateMessageSender(string queueName) => _sbConnectionStringBuilder.HasSasKey()
            ? new MessageSender(new ServiceBusConnection(_sbConnectionStringBuilder), queueName)
            : new MessageSender(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

        private IMessageReceiver CreateMessageReceiver(string queueName, int? prefetch = null)
        {
            if (_messageReceiver != null) return _messageReceiver;

            lock (_padlock)
            {
                if (_messageReceiver != null) return _messageReceiver;

                _messageReceiver = _sbConnectionStringBuilder.HasSasKey()
                    ? new MessageReceiver(new ServiceBusConnection(_sbConnectionStringBuilder), queueName)
                    : new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

                if (prefetch.HasValue)
                {
                    _messageReceiver.PrefetchCount = prefetch.Value;
                }
            }

            return _messageReceiver;
        }
    }
}