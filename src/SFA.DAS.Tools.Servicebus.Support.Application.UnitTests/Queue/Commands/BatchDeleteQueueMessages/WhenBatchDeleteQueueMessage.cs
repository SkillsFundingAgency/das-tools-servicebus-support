using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Commands.DeleteQueueMessage
{
    public class WhenBatchDeleteQueueMessage
    {
        private Mock<ICosmosMessageDbContext> _cosmosDbContext;
        private readonly Mock<ILogger<BatchDeleteQueueMessagesCommandHandler>> _logger = new Mock<ILogger<BatchDeleteQueueMessagesCommandHandler>>();
        private IList<string> _messageIds;
        private const int BatchSize = 20;

        [Test]
        public async Task ThenWillCallServiceToDeleteMessage()
        {
            _messageIds = new List<string>();
            var i = 0;
            while (i++ < 30)
            {
                _messageIds.Add($"id{i}");
            }

            _cosmosDbContext = new Mock<ICosmosMessageDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.DeleteQueueMessagesAsync(_messageIds)).Returns(Task.CompletedTask);

            var sut = new BatchDeleteQueueMessagesCommandHandler(_cosmosDbContext.Object, BatchSize, _logger.Object);

            await sut.Handle(new BatchDeleteQueueMessagesCommand()
            {
                Ids = _messageIds
            });

            _cosmosDbContext.Verify(x => x.DeleteQueueMessagesAsync(_messageIds), Times.Exactly(2));
        }
    }
}
