using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetQueueDetails
{
    public class WhenGettingQueueMessageCount
    {
        private readonly string _queueName = "1";
        private Mock<IAsbService> _asbService;
        private readonly long _expectedCount = 42;

        [Test]
        public async Task ThenWillGetQueueDetailsFromService()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.GetQueueMessageCountAsync(_queueName)).ReturnsAsync(_expectedCount);

            var sut = new GetQueueMessageCountQueryHandler(_asbService.Object);

            await sut.Handle(new GetQueueMessageCountQuery()
            {
                QueueName = _queueName
            });

            _asbService.Verify(x => x.GetQueueMessageCountAsync(_queueName), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.GetQueueMessageCountAsync(_queueName)).ReturnsAsync(_expectedCount);

            var sut = new GetQueueMessageCountQueryHandler(_asbService.Object);

            var response = await sut.Handle(new GetQueueMessageCountQuery()
            {
                QueueName = _queueName
            });

            response.Should().NotBeNull();
            response.Count.Should().Be(_expectedCount);
        }
    }
}
