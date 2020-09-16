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
        private QueueMessage _msg;

        [Test]
        public async Task ThenWillCallServiceToDeleteMessage()
        {
            _msg = new QueueMessage();
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.DeleteQueueMessageAsync(_msg)).Returns(Task.CompletedTask);

            var sut = new DeleteQueueMessageCommandHandler(_cosmosDbContext.Object);

            await sut.Handle(new DeleteQueueMessageCommand()
            {
                Message = _msg
            });

            _cosmosDbContext.Verify(x => x.DeleteQueueMessageAsync(_msg), Times.Once);
        }
    }
}
