using NUnit.Framework;
using System.Threading.Tasks;
using Moq;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using FluentAssertions;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetMessage
{
    public class WhenGettingAMessage
    {
        private readonly string _userId = "1";
        private readonly string _messageId = "1234";
        private Mock<ICosmosDbContext> _cosmosDbContext;

        public WhenGettingAMessage()
        {
        }

        [Test]
        public async Task ThenWillGetAMessageFromService()
        {
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.GetQueueMessageAsync(_userId, _messageId)).ReturnsAsync(new QueueMessage());

            var sut = new GetMessageQueryHandler(_cosmosDbContext.Object);

            await sut.Handle(new GetMessageQuery()
            {
                MessageId = _messageId,
                UserId = _userId
            });

            _cosmosDbContext.Verify(x => x.GetQueueMessageAsync(_userId, _messageId), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.GetQueueMessageAsync(_userId, _messageId)).ReturnsAsync(new QueueMessage());

            var sut = new GetMessageQueryHandler(_cosmosDbContext.Object);

            var response = await sut.Handle(new GetMessageQuery()
            {
                MessageId = _messageId,
                UserId = _userId
            });

            response.Should().NotBeNull();
            response.Message.Should().NotBeNull();
        }
    }
}
