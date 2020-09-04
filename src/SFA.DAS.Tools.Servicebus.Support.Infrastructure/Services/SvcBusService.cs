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
        Task<IEnumerable<string>> GetErrorQueuesAsync();
        Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty);
        Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty);                
        Task SendMessageToErrorQueueAsync(QueueMessage msg);
        Task SendMessageToProcessingQueueAsync(QueueMessage msg);
    }

    public class SvcBusService : ISvcBusService
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public readonly string serviceBusConnectionString;
        private readonly int batchSize;

        public SvcBusService(IConfiguration config, ILogger<SvcBusService> logger)
        {
            _config = config ?? throw new Exception("config is null");
            _logger = logger ?? throw new Exception("logger is null");

            serviceBusConnectionString = _config.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
            batchSize = _config.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize");
        }

        public async Task<IEnumerable<string>> GetErrorQueuesAsync()
        {            
            var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            var sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            var managementClient = new ManagementClient(sbConnectionStringBuilder, tokenProvider);

            var queues = await managementClient.GetQueuesAsync().ConfigureAwait(false);
            var regexString = _config.GetValue<string>("ServiceBusRepoSettings:QueueSelectionRegex");
            var queueSelectionRegex = new Regex(regexString);
            var errorQueues = queues.Where(q => queueSelectionRegex.IsMatch(q.Path)).Select(x => x.Path);

            return errorQueues;
        }

        public async Task<IEnumerable<QueueMessage>> PeekMessagesAsync(string queueName, int qty)
        {                                   
            var sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            var messageReceiver = new MessageReceiver(sbConnectionStringBuilder.Endpoint, queueName, tokenProvider);

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
                            id = Guid.NewGuid(),
                            userId = "123456",
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

        public async Task<IEnumerable<QueueMessage>> ReceiveMessagesAsync(string queueName, int qty)
        {            
            var sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
            var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            var messageReceiver = new MessageReceiver(sbConnectionStringBuilder.Endpoint, queueName, tokenProvider);


            int totalMessages = 0;
            IList<Message> receivedMessages;
            var formattedMessages = new List<QueueMessage>();

            var messageQtyToGet = CalculateMessageQtyToGet(qty, 0, batchSize);

            receivedMessages = await messageReceiver.ReceiveAsync(messageQtyToGet);

            if ( receivedMessages != null)
            {
                _logger.LogDebug($"Received Message Count: {receivedMessages.Count}");

                while (receivedMessages.Count > 0 || totalMessages < qty)
                {
                    totalMessages += receivedMessages.Count;
                    foreach (var msg in receivedMessages)
                    {
                        formattedMessages.Add(new QueueMessage
                        {
                            id = Guid.NewGuid(),
                            userId = "123456",
                            OriginalMessage = msg,
                            Queue = queueName,
                            IsReadOnly = false
                        });
                    }
                    messageQtyToGet = CalculateMessageQtyToGet(qty, totalMessages, batchSize);
                    receivedMessages = await messageReceiver.ReceiveAsync(messageQtyToGet);
                }
            }
            
            await messageReceiver.CloseAsync();

            return formattedMessages;
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
            var sbConnectionString = _config.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");

            var sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(sbConnectionString);
            var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            var messageSender = new MessageSender(sbConnectionStringBuilder.Endpoint, queueName, tokenProvider);

            if (!errorMessage.IsReadOnly)
            {
                await messageSender.SendAsync(errorMessage.OriginalMessage);
            }
        }        
    }
}
