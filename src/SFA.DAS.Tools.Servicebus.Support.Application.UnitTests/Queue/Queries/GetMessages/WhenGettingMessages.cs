using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetMessages
{
    public class WhenGettingQueueDetails
    {
        private readonly string _queueName = "queue";
        private Mock<IAsbService> _asbService;

        [Test]
        public async Task ThenWillGetQueueDetailsFromService()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.GetQueueDetailsAsync(_queueName)).ReturnsAsync(new QueueInfo());

            var sut = new GetQueueDetailsQueryHandler(_asbService.Object);

            await sut.Handle(new GetQueueDetailsQuery()
            {
                QueueName = _queueName
            });

            _asbService.Verify(x => x.GetQueueDetailsAsync(_queueName), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.GetQueueDetailsAsync(_queueName)).ReturnsAsync(new QueueInfo());

            var sut = new GetQueueDetailsQueryHandler(_asbService.Object);

            var response = await sut.Handle(new GetQueueDetailsQuery()
            {
                QueueName = _queueName
            });

            response.Should().NotBeNull();
            response.QueueInfo.Should().NotBeNull();
        }
    }
}
