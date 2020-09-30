using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service = SFA.DAS.Tools.Servicebus.Support.Application.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Services.MessageService
{
    public class WhenProcessingMessagesFromQueue
    {
        private readonly string _queueName = "q";
        private readonly int _qty = 20;
        private readonly int _batchSize = 9;

        private readonly Mock<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>> _bulkCreateQueueMessagesCommand = new Mock<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>>();

        private readonly Mock<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>>
            _receiveQueueMessagesQuery =
                new Mock<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>>();

        private readonly Mock<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>>
            _getQueueMessageCountQuery =
                new Mock<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>>();

        private readonly Mock<ILogger<Service.MessageService>>
            _iLogger =
                new Mock<ILogger<Service.MessageService>>();

        private readonly Mock<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>
            _sendMessagesCommand = new Mock<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>();

        private readonly Mock<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>
            _deleteMessagesCommand = new Mock<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>();

        [Test]
        public async Task ThenTheMessagesAreRequestedFromTheQueueAndSentToTheDatabase()
        {
            _bulkCreateQueueMessagesCommand.Setup(x => x.Handle(It.IsAny<BulkCreateQueueMessagesCommand>())).ReturnsAsync(new BulkCreateQueueMessagesCommandResponse());
            _getQueueMessageCountQuery.Setup(x => x.Handle(It.IsAny<GetQueueMessageCountQuery>())).ReturnsAsync(new GetQueueMessageCountQueryResponse() { Count = _qty });
            _receiveQueueMessagesQuery.Setup(x => x.Handle(It.IsAny<ReceiveQueueMessagesQuery>())).ReturnsAsync(() =>
            {
                var messages = new List<QueueMessage>(_batchSize);
                var i = 0;
                while (i++ < _batchSize)
                {
                    messages.Add(new QueueMessage());
                }

                return new ReceiveQueueMessagesQueryResponse()
                {
                    Messages = messages
                };
            });

            var sut = new Service.MessageService(_bulkCreateQueueMessagesCommand.Object, _receiveQueueMessagesQuery.Object, _getQueueMessageCountQuery.Object, new BatchGetMessageStrategy(), new BatchSendMessageStrategy(), _iLogger.Object, _batchSize, _sendMessagesCommand.Object, _deleteMessagesCommand.Object);

           await sut.GetMessages(_queueName);

           _bulkCreateQueueMessagesCommand.Verify(x => x.Handle(It.IsAny<BulkCreateQueueMessagesCommand>()), Times.Exactly(3));
           _getQueueMessageCountQuery.Verify(x => x.Handle(It.IsAny<GetQueueMessageCountQuery>()), Times.Once);
           _receiveQueueMessagesQuery.Verify(x => x.Handle(It.IsAny<ReceiveQueueMessagesQuery>()), Times.Exactly(3));
        }
    }
}
