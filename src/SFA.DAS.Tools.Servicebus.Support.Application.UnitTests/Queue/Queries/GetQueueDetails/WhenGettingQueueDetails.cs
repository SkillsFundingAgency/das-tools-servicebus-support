using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetQueueDetails
{
    public class WhenGettingMessages
    {
        private readonly string _userId = "1";
        private Mock<ICosmosMessageDbContext> _cosmosDbContext;
        private readonly SearchProperties _searchProperties = new SearchProperties();

        [Test]
        public async Task ThenWillGetQueueDetailsFromService()
        {
            _cosmosDbContext = new Mock<ICosmosMessageDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.GetQueueMessagesAsync(_userId, _searchProperties)).ReturnsAsync(new List<QueueMessage>());
            _cosmosDbContext.Setup(x => x.GetMessageCountAsync(_userId, It.IsAny<SearchProperties>())).ReturnsAsync(1);

            var sut = new GetMessagesQueryHandler(_cosmosDbContext.Object);

            await sut.Handle(new GetMessagesQuery()
            {
                UserId = _userId,
                SearchProperties = _searchProperties
            });

            _cosmosDbContext.Verify(x => x.GetQueueMessagesAsync(_userId, _searchProperties), Times.Once);
            _cosmosDbContext.Verify(x => x.GetMessageCountAsync(_userId, It.IsAny<SearchProperties>()), Times.Exactly(2));
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _cosmosDbContext = new Mock<ICosmosMessageDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.GetQueueMessagesAsync(_userId, _searchProperties)).ReturnsAsync(new List<QueueMessage>());
            _cosmosDbContext.Setup(x => x.GetMessageCountAsync(_userId, It.IsAny<SearchProperties>())).ReturnsAsync(1);

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
