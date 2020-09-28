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
        private readonly TokenProvider _tokenProvider;
        private readonly ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private readonly ManagementClient _managementClient;
        private readonly IUserService _userService;

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
            
            _tokenProvider = tokenProvider;
            _sbConnectionStringBuilder = serviceBusConnectionStringBuilder;
            _managementClient = managementClient;
            _userService = userService;
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

        public async Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty)
        {
            var messageReceiver = CreateMessageReceiver(queueName);
            var messages = await messageReceiver.PeekAsync(qty);
            var formattedMessages = new List<QueueMessage>(messages.Count);

            foreach (var message in messages)
            {
                formattedMessages.Add(CreateQueueMessage(message, queueName));
            }
            await messageReceiver.CloseAsync();

            return formattedMessages;
        }

        public async Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty)
        {
            var messageReceiver = CreateMessageReceiver(queueName);
            var messages = await messageReceiver.ReceiveAsync(qty);
            var formattedMessages = new List<QueueMessage>(messages.Count);

            foreach (var message in messages)
            {
                await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
                formattedMessages.Add(CreateQueueMessage(message, queueName));
            }

            return formattedMessages;
        }        

        public async Task<long> GetQueueMessageCountAsync(string queueName) => (await GetQueueDetailsAsync(queueName)).MessageCount;

        private IMessageReceiver CreateMessageReceiver(string queueName)
        {
            if ( _sbConnectionStringBuilder.SasKey?.Length > 0)
            {
                return new MessageReceiver(new ServiceBusConnection(_sbConnectionStringBuilder),queueName);
            }
            else
            {
                return new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);
            }                                  
        }

        public async Task SendMessagesAsync(IEnumerable<QueueMessage> messages, string queueName)
        {
            MessageSender messageSender = GetMessageSender(queueName);

            if (messages.Count() > 0)
            {
                var orginalMessages = new List<Message>();
                try
                {
                    orginalMessages = messages.Select(m => m.OriginalMessage).ToList();
                    await messageSender.SendAsync(orginalMessages);
                }catch(MessageSizeExceededException)
                {
                    _logger.LogDebug("SendMessagesAsync MessageSizeExceededException");
                    foreach(var msg in orginalMessages)
                    {
                        await messageSender.SendAsync(msg);
                    }                                                                
                }
                
            }
        }

        private MessageSender GetMessageSender(string queueName)
        {
            if (_sbConnectionStringBuilder.SasKey?.Length > 0)
            {
                return new MessageSender(new ServiceBusConnection(_sbConnectionStringBuilder), queueName);
            }
            else
            {
                return new MessageSender(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);
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

    }
}
