using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService
{
    public interface ISvcBusService
    {
        Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync();
        Task<QueueInfo> GetQueueDetailsAsync(string name);
        Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty);
        Task<ReceiveMessagesResponse> ReceiveMessagesAsync(string queueName, int qty);
        Task SendMessagesAsync(IEnumerable<QueueMessage> messages, string queueName);        
        Task Complete(MessageReceiver messageReceiver, IEnumerable<string> lockTokens);
        Task Complete(MessageReceiver messageReceiver, string lockToken);
        Task CloseMessageReceiver(MessageReceiver messageReceiver);
    }

    public class SvcBusService : ISvcBusService
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public readonly string serviceBusConnectionString;
        private readonly int batchSize;

        private TokenProvider _tokenProvider;
        private ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private ManagementClient _managementClient;

        public SvcBusService(IConfiguration config, ILogger<SvcBusService> logger)
        {
            _config = config ?? throw new Exception("config is null");
            _logger = logger ?? throw new Exception("logger is null");

            serviceBusConnectionString = _config.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
            batchSize = _config.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize");

            _tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            _sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            _managementClient = new ManagementClient(_sbConnectionStringBuilder, _tokenProvider);
        }

        public async Task<IEnumerable<QueueInfo>> GetErrorMessageQueuesAsync()
        {
            var queues = new List<QueueInfo>();

            var queuesDetails = await _managementClient.GetQueuesRuntimeInfoAsync().ConfigureAwait(false);
            var regexString = _config.GetValue<string>("ServiceBusRepoSettings:QueueSelectionRegex");
            var queueSelectionRegex = new Regex(regexString);
            var errorQueues = queuesDetails.Where(q => queueSelectionRegex.IsMatch(q.Path));//.Select(x => x.Path);

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

            var messageReceiver = new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

            int totalMessages = 0;
            IList<Message> peekedMessages;
            var formattedMessages = new List<QueueMessage>();

            var messageQtyToGet = CalculateMessageQtyToGet(qty, 0, batchSize);
            peekedMessages = messageQtyToGet > 0 ? await messageReceiver.PeekAsync(messageQtyToGet) : null;

            _logger.LogDebug($"Peeked Message Count: {peekedMessages.Count}");
            if (peekedMessages != null)
            {
                while (peekedMessages?.Count > 0 && totalMessages < qty)
                {
                    totalMessages += peekedMessages.Count;
                    foreach (var msg in peekedMessages)
                    {
                        formattedMessages.Add(new QueueMessage
                        {
                            Id = msg.MessageId,
                            UserId = UserService.GetUserId(),
                            OriginalMessage = msg,
                            Queue = queueName,
                            IsReadOnly = true,
                            Body = Encoding.UTF8.GetString(msg.Body),
                            OriginatingEndpoint = msg.UserProperties["NServiceBus.OriginatingEndpoint"].ToString(),
                            ProcessingEndpoint = msg.UserProperties["NServiceBus.ProcessingEndpoint"].ToString(),
                            Exception = msg.UserProperties["NServiceBus.ExceptionInfo.Message"].ToString(),
                            ExceptionType = msg.UserProperties["NServiceBus.ExceptionInfo.ExceptionType"].ToString()
                        });
                    }
                    messageQtyToGet = CalculateMessageQtyToGet(qty, totalMessages, batchSize);                    
                    peekedMessages = messageQtyToGet > 0 ? await messageReceiver.PeekAsync(messageQtyToGet) : null;
                }
            }

            await messageReceiver.CloseAsync();

            return formattedMessages;
        }

        public async Task<ReceiveMessagesResponse> ReceiveMessagesAsync(string queueName, int qty)
        {
            var messageReceiver = new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);


            int totalMessages = 0;
            IList<Message> receivedMessages;
            var formattedMessages = new List<QueueMessage>();

            var queueInfo = await GetQueueDetailsAsync(queueName);
            if( queueInfo.MessageCount < qty)
            {
                qty = Convert.ToInt32(queueInfo.MessageCount);
            }

            var messageQtyToGet = CalculateMessageQtyToGet(qty, 0, batchSize);
            receivedMessages = messageQtyToGet > 0 ? await messageReceiver.ReceiveAsync(messageQtyToGet) : null;

            if (receivedMessages != null)
            {
                _logger.LogDebug($"Received Message Count: {receivedMessages.Count}");

                while (receivedMessages?.Count > 0 && totalMessages < qty)
                {
                    totalMessages += receivedMessages.Count;
                    foreach (var msg in receivedMessages)
                    {
                        formattedMessages.Add(new QueueMessage
                        {
                            Id = msg.MessageId,
                            UserId = UserService.GetUserId(),
                            OriginalMessage = msg,
                            Queue = queueName,
                            IsReadOnly = false,
                            Body = Encoding.UTF8.GetString(msg.Body),
                            OriginatingEndpoint = msg.UserProperties["NServiceBus.OriginatingEndpoint"].ToString(),
                            ProcessingEndpoint = msg.UserProperties["NServiceBus.ProcessingEndpoint"].ToString(),
                            Exception = msg.UserProperties["NServiceBus.ExceptionInfo.Message"].ToString(),
                            ExceptionType = msg.UserProperties["NServiceBus.ExceptionInfo.ExceptionType"].ToString()
                        });
                        await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
                    }

                    messageQtyToGet = CalculateMessageQtyToGet(qty, totalMessages, batchSize);
                    receivedMessages = messageQtyToGet > 0 ? await messageReceiver.ReceiveAsync(messageQtyToGet) : null;
                }
            }

            //await messageReceiver.CloseAsync();

            return new ReceiveMessagesResponse()
            {
                Messages = formattedMessages,
                MessageReceiver = messageReceiver
            };
        }
      

        private int CalculateMessageQtyToGet(int totalExpected, int received, int batchSize)
        {
            var qtyRequired = totalExpected - received;

            if (qtyRequired >= batchSize)
                return batchSize;
            else if (qtyRequired < batchSize && qtyRequired > 0)
                return qtyRequired;
            return 0;
        }

        public async Task SendMessagesAsync(IEnumerable<QueueMessage> messages, string queueName)
        {            
            var messageSender = new MessageSender(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

            if (messages.Count() > 0)
            {
                var orginalMessages = messages.Select(m => m.OriginalMessage).ToList();
                await messageSender.SendAsync(orginalMessages);
            }
        }

        public async Task Complete(MessageReceiver messageReceiver, IEnumerable<string> lockTokens)
        {
            try
            {
                if (lockTokens?.Count() > 0)
                    await messageReceiver.CompleteAsync(lockTokens).ConfigureAwait(false);

            }catch(Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }
        }

        public async Task Complete(MessageReceiver messageReceiver, string lockToken)
        {
            if (!string.IsNullOrEmpty(lockToken))
                await messageReceiver.CompleteAsync(lockToken).ConfigureAwait(false);
        }

        public async Task CloseMessageReceiver(MessageReceiver messageReceiver)
        {
            if (!messageReceiver.IsClosedOrClosing)
                await messageReceiver.CloseAsync();
        }
    }
}
