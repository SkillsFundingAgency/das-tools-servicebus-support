using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Service = SFA.DAS.Tools.Servicebus.Support.Application.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Services.MessageService
{
    public class WhenReplayingMessages
    {

        private readonly Mock<IBatchSendMessageStrategy> _batchSendMessageStrategy = new Mock<IBatchSendMessageStrategy>();
        private readonly Mock<ILogger<Service.MessageService>> _logger = new Mock<ILogger<Service.MessageService>>();
        private readonly Mock<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>> _sendMessagesCommand = new Mock<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>();
        private readonly Mock<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>> _deleteQueueMessageCommand = new Mock<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>();

        [SetUp]
        public void Setup()
        {
            _sendMessagesCommand.Invocations.Clear();
            _deleteQueueMessageCommand.Invocations.Clear();
        }


        [Test]
        public async Task ThenReplayMessagesShouldCallSendAndDeleteCommands()
        {
            var messages = new List<QueueMessage>();
            var queueMessage = new QueueMessage
            {
                OriginalMessage = new Message(Encoding.UTF8.GetBytes("{}")) { MessageId = Guid.NewGuid().ToString() }
            };
            messages.Add(queueMessage);
            _sendMessagesCommand.Setup(x => x.Handle(It.IsAny<SendMessagesCommand>()))
                .ReturnsAsync(new SendMessagesCommandResponse());
            _deleteQueueMessageCommand.Setup(x => x.Handle(It.IsAny<DeleteQueueMessagesCommand>()))
                .ReturnsAsync(new DeleteQueueMessagesCommandResponse());

            var sut = new Service.MessageService(
                new BatchSendMessageStrategy(),
                _logger.Object,
                _sendMessagesCommand.Object,
                _deleteQueueMessageCommand.Object
            );

            await sut.ReplayMessages(messages, "test");

            _sendMessagesCommand.Verify(s => s.Handle(It.IsAny<SendMessagesCommand>()), Times.Once);
            _deleteQueueMessageCommand.Verify(s => s.Handle(It.IsAny<DeleteQueueMessagesCommand>()), Times.Once);
        }

        [Test]
        public async Task ThenAbortMessagesShouldCallSendAndDeleteCommands()
        {
            var messages = new List<QueueMessage>();

            for (var i = 0; i < 4; i++)
            {
                var queueMessage = new QueueMessage
                {
                    OriginalMessage = new Message(Encoding.UTF8.GetBytes("{}")) { MessageId = Guid.NewGuid().ToString() }
                };
                messages.Add(queueMessage);
            }
            _sendMessagesCommand.Setup(x => x.Handle(It.IsAny<SendMessagesCommand>()))
                .ReturnsAsync(new SendMessagesCommandResponse());
            _deleteQueueMessageCommand.Setup(x => x.Handle(It.IsAny<DeleteQueueMessagesCommand>()))
                .ReturnsAsync(new DeleteQueueMessagesCommandResponse());

            var sut = new Service.MessageService(
                new BatchSendMessageStrategy(),
                _logger.Object,
                _sendMessagesCommand.Object,
                _deleteQueueMessageCommand.Object
            );

            await sut.AbortMessages(messages, "test");

            _sendMessagesCommand.Verify(s => s.Handle(It.IsAny<SendMessagesCommand>()), Times.Once);
            _deleteQueueMessageCommand.Verify(s => s.Handle(It.IsAny<DeleteQueueMessagesCommand>()), Times.Once);
        }
    }
}
