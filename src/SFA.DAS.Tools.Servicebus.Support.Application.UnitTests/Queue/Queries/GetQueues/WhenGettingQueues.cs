using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetQueues
{
    public class WhenGettingQueues
    {
        private IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse> _sut;
        private Mock<IAsbService> _asbService;
        private Fixture Fixture = new Fixture();
        private ServiceBusErrorManagementSettings serviceBusErrorManagementSettings = new ServiceBusErrorManagementSettings { QueueSelectionRegex = "([-,_]+error)" };

        [SetUp]
        public void StartUp()
        {
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);

            _sut = new GetQueuesQueryHandler(_asbService.Object, serviceBusErrorManagementSettings);
        }

        [Test]
        public async Task ThenWillGetQueuesFromService()
        {
            // Arrange
            _asbService.Setup(x => x.GetMessageQueuesAsync(0, 100)).ReturnsAsync(new List<QueueInfo>());

            // Act
            await _sut.Handle(new GetQueuesQuery());

            // Assert
            _asbService.Verify(x => x.GetMessageQueuesAsync(0, 100), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            // Arrange
            _asbService.Setup(x => x.GetMessageQueuesAsync(0, 100)).ReturnsAsync(new List<QueueInfo>());

            // Act
            var response = await _sut.Handle(new GetQueuesQuery());

            // Assert
            response.Should().NotBeNull();
            response.Queues.Should().NotBeNull();
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(50, 49)]
        public async Task ThenWillReturnOnlyErrorQueuesForSinglePageOfQueues(int errorQueueCount, int nonErrorQueueCount)
        {
            // Maximum number of queues returned from the client in 1 hit is 100
            // Arrange
            var queueInfoFirstPage = new List<QueueInfo>();

            queueInfoFirstPage.AddRange(GetQueuesMockData(nonErrorQueueCount, false));
            queueInfoFirstPage.AddRange(GetQueuesMockData(errorQueueCount, true));
            _asbService.Setup(x => x.GetMessageQueuesAsync(0, 100)).ReturnsAsync(queueInfoFirstPage);
            // Act
            var response = await _sut.Handle(new GetQueuesQuery());

            // Assert
            response.Queues.Count().Should().Be(errorQueueCount);
        }

        [Test]
        [TestCase(90, 60)]
        [TestCase(110, 80)]
        public async Task ThenWillReturnOnlyErrorQueuesForMultiplePageOfQueues(int errorQueueCount, int nonErrorQueueCount)
        {
            // Arrange
            var queueInfoFirstPage = new List<QueueInfo>();
            var queueInfoSecondPage = new List<QueueInfo>();

            var nonErrorQueues = GetQueuesMockData(nonErrorQueueCount, false);
            var errorQueues = GetQueuesMockData(errorQueueCount, true);

            queueInfoFirstPage.AddRange(nonErrorQueues.Take(50));
            queueInfoFirstPage.AddRange(errorQueues.Take(50));

            queueInfoSecondPage.AddRange(nonErrorQueues.Skip(50));
            queueInfoSecondPage.AddRange(errorQueues.Skip(50));

            _asbService.Setup(x => x.GetMessageQueuesAsync(0, 100)).ReturnsAsync(queueInfoFirstPage);
            _asbService.Setup(x => x.GetMessageQueuesAsync(100, 100)).ReturnsAsync(queueInfoSecondPage);

            // Act
            var response = await _sut.Handle(new GetQueuesQuery());

            // Assert
            response.Queues.Count().Should().Be(errorQueueCount);
        }

        private IEnumerable<QueueInfo> GetQueuesMockData(int count, bool isErrorQueue)
        {
            return Fixture
                .Build<QueueInfo>()
                .With(x => x.Name, $"{Guid.NewGuid()}-queue{(isErrorQueue ? "-error" : "")}")
                .CreateMany(count);
        }
    }
}
