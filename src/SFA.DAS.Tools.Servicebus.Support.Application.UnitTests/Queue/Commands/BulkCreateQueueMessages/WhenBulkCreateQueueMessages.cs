using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Commands.BulkCreateQueueMessages
{
    public class WhenBulkCreateQueueMessages
    {
        private Mock<ICosmosMessageDbContext> _cosmosDbContext;
        private IList<QueueMessage> _msgs;

        [Test]
        public async Task ThenWillCallServiceBulkCreateMessages()
        {
            _msgs = new List<QueueMessage>();
            _cosmosDbContext = new Mock<ICosmosMessageDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.BulkCreateQueueMessagesAsync(_msgs)).Returns(Task.CompletedTask);

            var sut = new BulkCreateQueueMessagesCommandHandler(_cosmosDbContext.Object);

            await sut.Handle(new BulkCreateQueueMessagesCommand()
            {
                Messages = _msgs
            });

            _cosmosDbContext.Verify(x => x.BulkCreateQueueMessagesAsync(_msgs), Times.Once);
        }
    }
}
