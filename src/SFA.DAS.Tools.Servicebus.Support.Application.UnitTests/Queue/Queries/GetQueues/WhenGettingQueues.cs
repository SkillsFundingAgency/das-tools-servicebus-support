using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetQueues
{
    public class WhenGettingQueues
    {
        private readonly string _userId = "1";
        private Mock<IAsbService> _asbService;
        private readonly SearchProperties _searchProperties = new SearchProperties();

        [Test]
        public async Task ThenWillGetQueuesFromService()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.GetErrorMessageQueuesAsync()).ReturnsAsync(new List<QueueInfo>());

            var sut = new GetQueuesQueryHandler(_asbService.Object);

            await sut.Handle(new GetQueuesQuery());

            _asbService.Verify(x => x.GetErrorMessageQueuesAsync(), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.GetErrorMessageQueuesAsync()).ReturnsAsync(new List<QueueInfo>());

            var sut = new GetQueuesQueryHandler(_asbService.Object);

            var response = await sut.Handle(new GetQueuesQuery());

            response.Should().NotBeNull();
            response.Queues.Should().NotBeNull();
        }

        [Test]
        public async Task AndFilteringEmptyQueuesTheResponseWillContainNoEmptyQueues()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            var queue = new List<QueueInfo>
            {
                new QueueInfo
                {
                    MessageCount = 0,
                    Name = "TestQueue1"
                },new QueueInfo
                {
                    MessageCount = 0,
                    Name = "TestQueue2"
                },new QueueInfo
                {
                    MessageCount = 2,
                    Name = "TestQueue3"
                },new QueueInfo
                {
                    MessageCount = 3,
                    Name = "TestQueue4"
                },
            };

            _asbService.Setup(x => x.GetErrorMessageQueuesAsync()).ReturnsAsync(queue);

            var sut = new GetQueuesQueryHandler(_asbService.Object);

            var response = await sut.Handle(new GetQueuesQuery() { FilterEmptyQueues = true });

            response.Should().NotBeNull();
            response.Queues.Count().Should().Be(2);
        }

        [Test]
        public async Task AndNotFilteringEmptyQueuesTheResponseWillContainEmptyQueues()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            var queue = new List<QueueInfo>
            {
                new QueueInfo
                {
                    MessageCount = 0,
                    Name = "TestQueue1"
                },new QueueInfo
                {
                    MessageCount = 0,
                    Name = "TestQueue2"
                },new QueueInfo
                {
                    MessageCount = 2,
                    Name = "TestQueue3"
                },new QueueInfo
                {
                    MessageCount = 3,
                    Name = "TestQueue4"
                },
            };

            _asbService.Setup(x => x.GetErrorMessageQueuesAsync()).ReturnsAsync(queue);

            var sut = new GetQueuesQueryHandler(_asbService.Object);

            var response = await sut.Handle(new GetQueuesQuery() { FilterEmptyQueues = false });

            response.Should().NotBeNull();
            response.Queues.Count().Should().Be(4);
        }
    }
}
