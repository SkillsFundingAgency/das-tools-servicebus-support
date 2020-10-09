using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService
{
    public class AsbService : IAsbService
    {
        private readonly ILogger<AsbService> _logger;
        private readonly TokenProvider _tokenProvider;
        private readonly ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private readonly ManagementClient _managementClient;
        private readonly IUserService _userService;
        private readonly string _regexString;
        private const int RetryTimeout = 200;
        private volatile IMessageReceiver _messageReceiver;
        private readonly object _padlock = new object();

        public AsbService(IUserService userService,
            IConfiguration config,
            ILogger<AsbService> logger,
            TokenProvider tokenProvider,
            ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder,
            ManagementClient managementClient
        )
        {
            _logger = logger ?? throw new Exception("logger is null");

            _regexString = config.GetValue<string>("ServiceBusRepoSettings:QueueSelectionRegex");
            _tokenProvider = tokenProvider;
            _sbConnectionStringBuilder = serviceBusConnectionStringBuilder;
            _managementClient = managementClient;
            _userService = userService;
        }

        public async Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync()
        {
            var queuesDetails = await _managementClient.GetQueuesRuntimeInfoAsync().ConfigureAwait(false);
            var queueSelectionRegex = new Regex(_regexString);
            var errorQueues = queuesDetails.Where(q => queueSelectionRegex.IsMatch(q.Path));

            return errorQueues.Select(queue => new QueueInfo()
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

            var queue = await _managementClient.GetQueueRuntimeInfoAsync(name).ConfigureAwait(false);

            return new QueueInfo()
            {
                Name = queue.Path,
                MessageCount = queue.MessageCountDetails.ActiveMessageCount
            };
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

            await Policy.Handle<Exception>().WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(RetryTimeout), HandleRetryException)
            .ExecuteAsync(async token =>
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

        private void HandleRetryException(Exception ex, TimeSpan timeSpan, Context context)
        {
            _logger.LogError(ex, $"Failed to Send messages to queue: {ex.ToString()}");
            throw ex;
        }
    }
}
