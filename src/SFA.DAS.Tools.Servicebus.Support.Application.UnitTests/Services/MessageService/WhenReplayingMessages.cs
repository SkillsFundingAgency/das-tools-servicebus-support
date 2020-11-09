using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Audit;
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


        [Test]
        public async Task ThenAuditServiceShouldBeCalled()
        {
            var _auditService = new Mock<IAuditService>();
            var messages = new List<QueueMessage>();
            var queueMessage = new QueueMessage
            {
                OriginalMessage = new Message(Encoding.UTF8.GetBytes("{}")) { MessageId = Guid.NewGuid().ToString() }
            };
            messages.Add(queueMessage);

            _auditService.Setup(x => x.WriteAudit(It.IsAny<MessageQueueReplayAuditMessage>()));

            var sut = new Service.MessageService(
                new BatchSendMessageStrategy(),
                _logger.Object,
                _sendMessagesCommand.Object,
                _deleteQueueMessageCommand.Object,
                _auditService.Object
            );

            await sut.ReplayMessages(messages, "test");

            _auditService.Verify(s => s.WriteAudit(It.IsAny<MessageQueueReplayAuditMessage>()), Times.Once);
        }

        [Test]
        public async Task ThenAuditServiceShouldBeCalledOnceForEveryMessage()
        {
            var _auditService = new Mock<IAuditService>();
            var messages = new List<QueueMessage>();

            for (var i = 0; i < 4; i++)
            {
                var queueMessage = new QueueMessage
                {
                    OriginalMessage = new Message(Encoding.UTF8.GetBytes("{}")) { MessageId = Guid.NewGuid().ToString() }
                };
                messages.Add(queueMessage);
            }

            _auditService.Setup(x => x.WriteAudit(It.IsAny<MessageQueueReplayAuditMessage>()));

            var sut = new Service.MessageService(
                new BatchSendMessageStrategy(),
                _logger.Object,
                _sendMessagesCommand.Object,
                _deleteQueueMessageCommand.Object,
                _auditService.Object
            );

            await sut.ReplayMessages(messages, "test");

            _auditService.Verify(s => s.WriteAudit(It.IsAny<MessageQueueReplayAuditMessage>()), Times.Exactly(4));
        }
    }
}
