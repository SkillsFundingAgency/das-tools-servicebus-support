using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.ReceiveQueueMessages
{
    public class WhenReceivingQueueMessages
    {
        private readonly string _queueName = "queue";
        private readonly int _qty = 10;
        private Mock<IAsbService> _asbService;

        [Test]
        public async Task ThenWillPeekQueueMessagesFromService()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.ReceiveMessagesAsync(_queueName, _qty)).ReturnsAsync(new List<QueueMessage>());

            var sut = new ReceiveQueueMessagesQueryHandler(_asbService.Object);

            await sut.Handle(new ReceiveQueueMessagesQuery()
            {
                QueueName = _queueName,
                Limit = _qty
            });

            _asbService.Verify(x => x.ReceiveMessagesAsync(_queueName, _qty), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.ReceiveMessagesAsync(_queueName, _qty)).ReturnsAsync(new List<QueueMessage>());

            var sut = new ReceiveQueueMessagesQueryHandler(_asbService.Object);

            var response = await sut.Handle(new ReceiveQueueMessagesQuery()
            {
                QueueName = _queueName,
                Limit = _qty
            });

            response.Should().NotBeNull();
            response.Messages.Should().NotBeNull();
        }
    }
}
