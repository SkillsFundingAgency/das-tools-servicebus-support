using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Commands.SendMessageToErrorQueue
{
    public class WhenSendMessageToErrorQueue
    {
        private Mock<IAsbService> _asbService;
        private IEnumerable<QueueMessage> _messages;
        private string _queueName;

        [Test]
        public async Task ThenWillCallServiceToSendMessage()
        {
            _messages = new List<QueueMessage>()
            {
                new QueueMessage()
            };
            _queueName = "name";
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.SendMessagesAsync(_messages,_queueName)).Returns(Task.CompletedTask);

            var sut = new SendMessagesCommandHandler(_asbService.Object);

            await sut.Handle(new SendMessagesCommand()
            {
                Messages = _messages,
                QueueName = _queueName
            });

            _asbService.Verify(x => x.SendMessagesAsync(_messages,_queueName), Times.Once);
        }
    }
}
