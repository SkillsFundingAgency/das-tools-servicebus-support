using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Core;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        Task SendMessageToErrorQueueAsync(QueueMessage msg);
        Task SendMessageToProcessingQueueAsync(QueueMessage msg);
        Task Complete(MessageReceiver messageReceiver, IEnumerable<string> lockTokens);
        Task Complete(MessageReceiver messageReceiver, string lockToken);
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
            var queue = await _managementClient.GetQueueRuntimeInfoAsync(name).ConfigureAwait(false);

            return new QueueInfo()
            {
                Name = queue.Path,
                MessageCount = queue.MessageCount
            };
        }

        public async Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty)
        {

            var messageReceiver = new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

            int totalMessages = 0;
            IList<Message> peekedMessages;
            var formattedMessages = new List<QueueMessage>();

            var messageQtyToGet = CalculateMessageQtyToGet(qty, 0, batchSize);
            peekedMessages = await messageReceiver.PeekAsync(messageQtyToGet);

            _logger.LogDebug($"Peeked Message Count: {peekedMessages.Count}");
            if (peekedMessages.Count > 0)
            {
                while (totalMessages < qty)
                {
                    totalMessages += peekedMessages.Count;
                    foreach (var msg in peekedMessages)
                    {
                        formattedMessages.Add(new QueueMessage
                        {
                            id = msg.MessageId,
                            userId = UserService.GetUserId(),
                            OriginalMessage = msg,
                            Queue = queueName,
                            IsReadOnly = true
                        });
                    }
                    messageQtyToGet = CalculateMessageQtyToGet(qty, totalMessages, batchSize);
                    peekedMessages = await messageReceiver.PeekAsync(messageQtyToGet);
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

            var messageQtyToGet = CalculateMessageQtyToGet(qty, 0, batchSize);

            receivedMessages = await messageReceiver.ReceiveAsync(messageQtyToGet);

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
                            id = msg.MessageId,
                            userId = UserService.GetUserId(),
                            OriginalMessage = msg,
                            Queue = queueName,
                            IsReadOnly = false
                        });
                        //await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
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

        public async Task SendMessageToErrorQueueAsync(QueueMessage msg)
        {
            await SendMessageAsync(msg, msg.Queue);
        }

        public async Task SendMessageToProcessingQueueAsync(QueueMessage msg)
        {
            var queueName = msg.OriginalMessage.UserProperties["NServiceBus.ProcessingEndpoint"].ToString();
            await SendMessageAsync(msg, queueName);
        }

        private int CalculateMessageQtyToGet(int totalExpected, int received, int batchSize)
        {
            var qtyRequried = totalExpected - received;

            if (qtyRequried >= batchSize)
                return batchSize;
            else if (qtyRequried < batchSize && qtyRequried > 0)
                return qtyRequried;
            return 0;
        }

        private async Task SendMessageAsync(QueueMessage errorMessage, string queueName)
        {

            var messageSender = new MessageSender(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);

            if (!errorMessage.IsReadOnly)
            {
                await messageSender.SendAsync(errorMessage.OriginalMessage);
            }
        }

        public async Task Complete(MessageReceiver messageReceiver, IEnumerable<string> lockTokens)
        {
            if (lockTokens.Count() > 0)
                await messageReceiver.CompleteAsync(lockTokens).ConfigureAwait(false);
        }

        public async Task Complete(MessageReceiver messageReceiver, string lockToken)
        {
            if (!string.IsNullOrEmpty(lockToken))
                await messageReceiver.CompleteAsync(lockToken).ConfigureAwait(false);
        }
    }
}
