using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Service = SFA.DAS.Tools.Servicebus.Support.Application.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Services.MessageService
{
    public class WhenProcessingMessagesFromQueue
    {
        private readonly string _queueName = "q";
        private readonly int _batchSize = 9;
        private const int GetQty = 3;
        private const string serviceBusConnectionString = "ServiceBusRepoSettings:ServiceBusConnectionString";

        private readonly Mock<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>>
            _bulkCreateQueueMessagesCommand =
                new Mock<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>>();

        private readonly Mock<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>>
            _receiveQueueMessagesQuery =
                new Mock<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>>();

        private readonly Mock<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>>
            _getQueueMessageCountQuery =
                new Mock<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>>();

        private readonly Mock<ILogger<Service.RetrieveMessagesService>>
            _iLogger =
                new Mock<ILogger<Service.RetrieveMessagesService>>();

        private readonly Mock<ITokenProvider> _tokenProvider = new Mock<ITokenProvider>();

        private readonly Mock<IUserService> _userService = new Mock<IUserService>();

        private readonly Mock<ICosmosMessageDbContext> _cosmosDbContext = new Mock<ICosmosMessageDbContext>();

        private readonly Mock<Service.IMessageReceiverFactory> _messageReceiverFactory =
            new Mock<Service.IMessageReceiverFactory>(MockBehavior.Strict);

        private readonly Mock<IMessageReceiver> _messageReceiver =
            new Mock<IMessageReceiver>(MockBehavior.Strict);

        [Test]
        public async Task ThenTheMessagesAreRequestedFromTheQueueAndSentToTheDatabase()
        {
            var messages = new List<Message>();
            for (var i = 0; i < 3; i++)
            {
                var m = new Message(Encoding.UTF8.GetBytes("{}")) { MessageId = Guid.NewGuid().ToString() };
                m.UserProperties.Add("NServiceBus.OriginatingEndpoint", "endpoint");
                m.UserProperties.Add("NServiceBus.ProcessingEndpoint", "endpoint");
                m.UserProperties.Add("NServiceBus.ExceptionInfo.Message", "Exception Message");
                m.UserProperties.Add("NServiceBus.ExceptionInfo.ExceptionType", "Exception Type");
                messages.Add(m);
            }

            _messageReceiver.SetupSet(receiver => receiver.PrefetchCount);
            _messageReceiver.Setup(receiver => receiver.ReceiveAsync(3, It.IsAny<TimeSpan>())).ReturnsAsync(messages);
            _messageReceiver.Setup(receiver => receiver.CompleteAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);
            _messageReceiver.Setup(receiver => receiver.CloseAsync()).Returns(Task.CompletedTask);
            _messageReceiverFactory.Setup(x => x.Create(_queueName)).Returns(_messageReceiver.Object);

            var sut = new Service.RetrieveMessagesService(
                _iLogger.Object,
                _batchSize,
                new BatchGetMessageStrategy(),
                _userService.Object,
                _cosmosDbContext.Object,                
                _messageReceiverFactory.Object
            );

            await sut.GetMessages(_queueName, 10, GetQty);

            _messageReceiver.VerifySet(receiver => receiver.PrefetchCount);
            _messageReceiver.Verify(receiver => receiver.ReceiveAsync(3, It.IsAny<TimeSpan>()), Times.Once);
            _messageReceiver.Verify(receiver => receiver.CompleteAsync(It.IsAny<IEnumerable<string>>()));
            _messageReceiver.Verify(receiver => receiver.CloseAsync());
        }
    }
}
