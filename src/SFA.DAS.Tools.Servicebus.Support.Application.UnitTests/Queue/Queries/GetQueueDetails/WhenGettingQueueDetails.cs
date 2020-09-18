using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;
using Moq;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using FluentAssertions;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetQueueDetails
{
    public class WhenGettingMessages
    {
        private readonly string _userId = "1";
        private Mock<ICosmosDbContext> _cosmosDbContext;
        private readonly SearchProperties _searchProperties = new SearchProperties();

        [Test]
        public async Task ThenWillGetQueueDetailsFromService()
        {
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.GetQueueMessagesAsync(_userId, _searchProperties)).ReturnsAsync(new List<QueueMessage>());
            _cosmosDbContext.Setup(x => x.GetMessageCountAsync(_userId, _searchProperties)).ReturnsAsync(1);

            var sut = new GetMessagesQueryHandler(_cosmosDbContext.Object);

            await sut.Handle(new GetMessagesQuery()
            {
                UserId = _userId,
                SearchProperties = _searchProperties
            });

            _cosmosDbContext.Verify(x => x.GetQueueMessagesAsync(_userId, _searchProperties), Times.Once);
            _cosmosDbContext.Verify(x => x.GetMessageCountAsync(_userId, _searchProperties), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.GetQueueMessagesAsync(_userId, _searchProperties)).ReturnsAsync(new List<QueueMessage>());
            _cosmosDbContext.Setup(x => x.GetMessageCountAsync(_userId, _searchProperties)).ReturnsAsync(1);

            var sut = new GetMessagesQueryHandler(_cosmosDbContext.Object);

            var response = await sut.Handle(new GetMessagesQuery()
            {
                UserId = _userId,
                SearchProperties = _searchProperties
            });

            response.Should().NotBeNull();
            response.Messages.Should().NotBeNull();
        }
    }
}
