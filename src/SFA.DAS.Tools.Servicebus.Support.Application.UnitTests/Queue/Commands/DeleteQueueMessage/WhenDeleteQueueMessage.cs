using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Commands.DeleteQueueMessage
{
    public class WhenDeleteQueueMessage
    {
        private Mock<ICosmosDbContext> _cosmosDbContext;
        private IEnumerable<string> _messageIds;

        [Test]
        public async Task ThenWillCallServiceToDeleteMessage()
        {
            _messageIds = new List<string>()
            {
                "id123"
            };
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.DeleteQueueMessagesAsync(_messageIds)).Returns(Task.CompletedTask);

            var sut = new DeleteQueueMessagesCommandHandler(_cosmosDbContext.Object);

            await sut.Handle(new DeleteQueueMessagesCommand()
            {
                Ids = _messageIds
            });

            _cosmosDbContext.Verify(x => x.DeleteQueueMessagesAsync(_messageIds), Times.Once);
        }
    }
}
