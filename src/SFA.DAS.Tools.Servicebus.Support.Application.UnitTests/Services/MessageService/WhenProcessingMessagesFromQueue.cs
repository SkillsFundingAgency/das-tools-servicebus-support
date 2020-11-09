using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
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
        private const int GetQty = 3;
        
        private readonly Mock<ILogger<Service.RetrieveMessagesService>>
            _iLogger =
                new Mock<ILogger<Service.RetrieveMessagesService>>();       

        private readonly Mock<IUserService> _userService = new Mock<IUserService>();

        private readonly Mock<ICosmosMessageDbContext> _cosmosDbContext = new Mock<ICosmosMessageDbContext>();

        private readonly Mock<Service.IMessageReceiverFactory> _messageReceiverFactory =
            new Mock<Service.IMessageReceiverFactory>(MockBehavior.Strict);

        private readonly Mock<IMessageReceiver> _messageReceiver =
            new Mock<IMessageReceiver>(MockBehavior.Strict);

        private readonly ServiceBusErrorManagementSettings serviceBusSettings = new ServiceBusErrorManagementSettings
        {
            PeekMessageBatchSize = 9
        };

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
                serviceBusSettings,
                _iLogger.Object,
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
