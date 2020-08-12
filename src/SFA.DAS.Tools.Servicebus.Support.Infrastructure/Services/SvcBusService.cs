using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Core;
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
        Task<IList<sbMessageModel>> PeekMessages(string queueName);
        Task<IEnumerable<string>> GetErrorQueuesAsync();
    }

    public class SvcBusService : ISvcBusService
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public SvcBusService(IConfiguration config, ILogger<SvcBusService> logger)
        {
            _config = config ?? throw new Exception("config is null");
            _logger = logger ?? throw new Exception("logger is null");
        }

        public async Task<IEnumerable<string>> GetErrorQueuesAsync()
        {

            var sbConnectionString = _config.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
            var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
            var sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(sbConnectionString);
            var managementClient = new ManagementClient(sbConnectionStringBuilder, tokenProvider);

            var queues = await managementClient.GetQueuesAsync().ConfigureAwait(false);         
            var regexString = _config.GetValue<string>("ServiceBusRepoSettings:QueueSelectionRegex");
            var queueSelectionRegex = new Regex(regexString);
            var errorQueues = queues.Where(q => queueSelectionRegex.IsMatch(q.Path)).Select(x => x.Path);            

#if DEBUG
            _logger.LogDebug("Error Queues:");
            foreach (var queue in errorQueues)
            {
                _logger.LogDebug(queue);
            }
#endif
            return errorQueues;
        }

        public Task<IList<sbMessageModel>> PeekMessages(string queueName)
        {
            throw new NotImplementedException();
        }

        //        public async Task<IList<sbMessageModel>> PeekMessages(string queueName)
        //        {
        //            var sbConnectionString = _config.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
        //            var batchSize = _config.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize");
        //            var notifyBatchSize = _config.GetValue<int>("ServiceBusRepoSettings:NotifyUIBatchSize");

        //            var sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(sbConnectionString);
        //            var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
        //            var messageReceiver = new MessageReceiver(sbConnectionStringBuilder.Endpoint, queueName, tokenProvider);

        //#if DEBUG
        //            _logger.LogDebug($"ServiceBusConnectionString: {sbConnectionString}");
        //            _logger.LogDebug($"PeekMessageBatchSize: {batchSize}");
        //#endif

        //            int totalMessages = 0;
        //            IList<Message> peekedMessages;
        //            var formattedMessages = new List<sbMessageModel>();

        //            peekedMessages = await messageReceiver.PeekAsync(batchSize);

        //            _logger.LogDebug($"Peeked Message Count: {peekedMessages.Count}");

        //            while (peekedMessages.Count > 0)
        //            {
        //                foreach (var msg in peekedMessages)
        //                {
        //                    var messageModel = FormatMsgToLog(msg);
        //                    totalMessages++;
        //                    if (totalMessages % notifyBatchSize == 0)
        //                        _logger.LogDebug($"    {queueName} - processed: {totalMessages}");

        //                    formattedMessages.Add(messageModel);
        //                }
        //                peekedMessages = await messageReceiver.PeekAsync(batchSize);
        //            }
        //            await messageReceiver.CloseAsync();

        //            return formattedMessages;
        //        }

        //        private sbMessageModel FormatMsgToLog(Message msg)
        //        {
        //            object exceptionMessage = string.Empty;
        //            string exceptionMessageNoCrLf = string.Empty;
        //            string enclosedMessageTypeTrimmed = string.Empty;
        //            var messageModel = new sbMessageModel();
        //            if (msg.UserProperties.TryGetValue("NServiceBus.ExceptionInfo.Message", out exceptionMessage))
        //            {
        //                // this is an nServiveBusFailure.
        //                exceptionMessageNoCrLf = exceptionMessage.ToString().CrLfToTilde();
        //                enclosedMessageTypeTrimmed = msg.UserProperties.ContainsKey("NServiceBus.EnclosedMessageTypes")
        //                    ? msg.UserProperties["NServiceBus.EnclosedMessageTypes"].ToString().Split(',')[0]
        //                    : "";

        //                messageModel.MessageId = msg.UserProperties["NServiceBus.MessageId"].ToString();
        //                messageModel.TimeOfFailure = msg.UserProperties["NServiceBus.TimeOfFailure"].ToString();
        //                messageModel.ExceptionType = msg.UserProperties["NServiceBus.ExceptionInfo.ExceptionType"].ToString();
        //                messageModel.OriginatingEndpoint = msg.UserProperties["NServiceBus.OriginatingEndpoint"].ToString();
        //                messageModel.ProcessingEndpoint = msg.UserProperties["NServiceBus.ProcessingEndpoint"].ToString();
        //                messageModel.EnclosedMessageTypes = enclosedMessageTypeTrimmed;
        //                messageModel.StackTrace = msg.UserProperties["NServiceBus.ExceptionInfo.StackTrace"].ToString().CrLfToTilde();
        //                messageModel.ExceptionMessage = exceptionMessageNoCrLf;
        //            }
        //            else if (msg.UserProperties.TryGetValue("DeadLetterReason", out exceptionMessage))
        //            {
        //                exceptionMessageNoCrLf = exceptionMessage.ToString().CrLfToTilde();
        //                enclosedMessageTypeTrimmed = msg.UserProperties.ContainsKey("NServiceBus.EnclosedMessageTypes")
        //                    ? msg.UserProperties["NServiceBus.EnclosedMessageTypes"].ToString().Split(',')[0]
        //                    : "";

        //                messageModel.MessageId = msg.UserProperties["NServiceBus.MessageId"].ToString();
        //                messageModel.TimeOfFailure = msg.UserProperties["NServiceBus.TimeSent"].ToString();
        //                messageModel.ExceptionType = "Unknown";
        //                messageModel.OriginatingEndpoint = msg.UserProperties["NServiceBus.OriginatingEndpoint"].ToString();
        //                messageModel.ProcessingEndpoint = "Unknown";
        //                messageModel.StackTrace = string.Empty;
        //                messageModel.EnclosedMessageTypes = enclosedMessageTypeTrimmed;
        //                messageModel.ExceptionMessage = exceptionMessageNoCrLf;
        //            }

        //#if DEBUG
        //            // When developing I want to be able to use as simple a message as possible but still see some information in the output
        //            // so I will just grab the message body and output it raw
        //            else
        //            {
        //                _logger.LogDebug($"msg.Body: {Encoding.UTF8.GetString(msg.Body)}");
        //                messageModel.RawMessage = Encoding.UTF8.GetString(msg.Body);
        //            }
        //#endif
        //            return messageModel;
        //        }
    }
}
