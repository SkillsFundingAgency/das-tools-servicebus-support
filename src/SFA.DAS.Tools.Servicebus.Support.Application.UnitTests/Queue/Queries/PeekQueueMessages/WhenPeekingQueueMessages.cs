using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.PeekQueueMessages
{
    public class WhenPeekingQueueMessages
    {
        private readonly string _queueName = "queue";
        private Mock<IAsbService> _asbService;

        [Test]
        public async Task ThenWillPeekQueueMessagesFromService()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.PeekMessagesAsync(_queueName)).ReturnsAsync(new List<QueueMessage>());

            var sut = new PeekQueueMessagesQueryHandler(_asbService.Object);

            await sut.Handle(new PeekQueueMessagesQuery()
            {
                QueueName = _queueName
            });

            _asbService.Verify(x => x.PeekMessagesAsync(_queueName), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.PeekMessagesAsync(_queueName)).ReturnsAsync(new List<QueueMessage>());

            var sut = new PeekQueueMessagesQueryHandler(_asbService.Object);

            var response = await sut.Handle(new PeekQueueMessagesQuery()
            {
                QueueName = _queueName
            });

            response.Should().NotBeNull();
            response.Messages.Should().NotBeNull();
        }
    }
}
