using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessageToErrorQueue;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Commands.SendMessageToErrorQueue
{
    public class WhenSendMessageToErrorQueue
    {
        private Mock<IAsbService> _asbService;
        private QueueMessage _msg;

        [Test]
        public async Task ThenWillCallServiceToSendMessage()
        {
            _msg = new QueueMessage();
            _asbService = new Mock<IAsbService>(MockBehavior.Strict);
            _asbService.Setup(x => x.SendMessageToErrorQueueAsync(_msg)).Returns(Task.CompletedTask);

            var sut = new SendMessageToErrorQueueCommandHandler(_asbService.Object);

            await sut.Handle(new SendMessageToErrorQueueCommand()
            {
                Message = _msg
            });

            _asbService.Verify(x => x.SendMessageToErrorQueueAsync(_msg), Times.Once);
        }
    }
}
