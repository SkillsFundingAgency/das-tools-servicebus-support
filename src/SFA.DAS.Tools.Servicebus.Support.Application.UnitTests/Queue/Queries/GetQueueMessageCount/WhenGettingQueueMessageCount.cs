using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;
using Moq;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using FluentAssertions;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

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
