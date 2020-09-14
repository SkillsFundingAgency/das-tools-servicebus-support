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
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService
{
    public class AsbService : IAsbService
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly int _batchSize;
        private readonly TokenProvider _tokenProvider;
        private readonly ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private readonly ManagementClient _managementClient;
        private readonly IUserService _userService;
        private readonly IBatchMessageStrategy _batchMessageStrategy;

        public AsbService(IUserService userService,
            IConfiguration config,
            ILogger<AsbService> logger,
            TokenProvider tokenProvider,
            ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder,
            ManagementClient managementClient,
            IBatchMessageStrategy batchMessageStrategy
        )
        {
            _config = config ?? throw new Exception("config is null");
            _logger = logger ?? throw new Exception("logger is null");
            
            _batchSize = _config.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize");
            _tokenProvider = tokenProvider;
            _sbConnectionStringBuilder = serviceBusConnectionStringBuilder;
            _managementClient = managementClient;
            _userService = userService;
            _batchMessageStrategy = batchMessageStrategy;
        }

        public async Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync()
        {
            var queues = new List<QueueInfo>();
            var queuesDetails = await _managementClient.GetQueuesRuntimeInfoAsync().ConfigureAwait(false);
            var regexString = _config.GetValue<string>("ServiceBusRepoSettings:QueueSelectionRegex");
            var queueSelectionRegex = new Regex(regexString);
            var errorQueues = queuesDetails.Where(q => queueSelectionRegex.IsMatch(q.Path));

            foreach (var queue in errorQueues)
            {
                queues.Add(new QueueInfo()
                {
                    Name = queue.Path,
                    MessageCount = queue.MessageCount
                });
            }

            return queues;
        }

        public async Task<QueueInfo> GetQueueDetailsAsync(string name)
        {
            var result = new QueueInfo();

            if (!string.IsNullOrEmpty(name))
            {
                var queue = await _managementClient.GetQueueRuntimeInfoAsync(name).ConfigureAwait(false);

                result.Name = queue.Path;
                result.MessageCount = queue.MessageCountDetails.ActiveMessageCount;
            }

            return result;
        }

        public async Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName)
        {
            var qty = await GetQueueMessageCount(queueName);
            var messageReceiver = CreateMessageReceiver(queueName);
            
            var messages = await _batchMessageStrategy.Execute(
                queueName,
                qty,
                _batchSize,
                async (messageQtyToGet) => await messageReceiver.PeekAsync(messageQtyToGet),
                async msg => CreateQueueMessage(msg, queueName));

            await messageReceiver.CloseAsync();

            return messages;
        }

        public async Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName)
        {
            var qty = await GetQueueMessageCount(queueName);
            var messageReceiver = CreateMessageReceiver(queueName);
            
            var messages = await _batchMessageStrategy.Execute(
                queueName,
                qty,
                _batchSize,
                async (messageQtyToGet) => await messageReceiver.ReceiveAsync(messageQtyToGet),
                async msg =>
                {
                    await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
                    return CreateQueueMessage(msg, queueName);

                });

            return messages;
        }

        public async Task SendMessageToErrorQueueAsync(QueueMessage msg) => await SendMessageAsync(msg, msg.Queue);

        public async Task SendMessageToProcessingQueueAsync(QueueMessage msg) => await SendMessageAsync(msg, msg.ProcessingEndpoint);

        private async Task SendMessageAsync(QueueMessage errorMessage, string queueName)
        {
            var messageSender = new MessageSender(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

            if (!errorMessage.IsReadOnly)
            {
                await messageSender.SendAsync(errorMessage.OriginalMessage);
            }
        }

        private QueueMessage CreateQueueMessage(Message message, string queueName)
        {
            return new QueueMessage
            {
                Id = message.MessageId,
                UserId = _userService.GetUserId(),
                OriginalMessage = message,
                Queue = queueName,
                IsReadOnly = false,
                Body = Encoding.UTF8.GetString(message.Body),
                OriginatingEndpoint = message.UserProperties["NServiceBus.OriginatingEndpoint"].ToString(),
                ProcessingEndpoint = message.UserProperties["NServiceBus.ProcessingEndpoint"].ToString(),
                Exception = message.UserProperties["NServiceBus.ExceptionInfo.Message"].ToString(),
                ExceptionType = message.UserProperties["NServiceBus.ExceptionInfo.ExceptionType"].ToString()
            };
        }

        private async Task<long> GetQueueMessageCount(string queueName)
        {
            var queueInfo = await GetQueueDetailsAsync(queueName);
            return queueInfo.MessageCount;
        }

        private IMessageReceiver CreateMessageReceiver(string queueName)
        {
            var messageReceiver = new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);
            return messageReceiver;
        }
    }
}
