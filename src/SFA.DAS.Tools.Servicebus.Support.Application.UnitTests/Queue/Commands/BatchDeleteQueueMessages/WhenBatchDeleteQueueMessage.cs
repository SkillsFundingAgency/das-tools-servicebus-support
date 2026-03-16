using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Commands.BatchDeleteQueueMessages;

public class WhenBatchDeleteQueueMessage
{
    private Mock<ICosmosMessageDbContext> _cosmosDbContext;
    private readonly Mock<ILogger<BatchDeleteQueueMessagesCommandHandler>> _logger = new();
    private IList<string> _messageIds;
    private readonly ServiceBusErrorManagementSettings _serviceBusSettings = new()
    {
        PeekMessageBatchSize = 20
    };
    private readonly Mock<IAuditService> _auditService = new(MockBehavior.Strict);

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
        var deletedBatches = new List<IReadOnlyCollection<string>>();
        _cosmosDbContext.Setup(x => x.DeleteQueueMessagesAsync(It.IsAny<IEnumerable<string>>()))
            .Callback<IEnumerable<string>>(ids => deletedBatches.Add(ids.ToList()))
            .Returns(Task.CompletedTask);
        _auditService.Setup(x => x.WriteAudit(It.IsAny<MessageQueueDeleteAuditMessage>()));

        var sut = new BatchDeleteQueueMessagesCommandHandler(_cosmosDbContext.Object, _serviceBusSettings, _logger.Object, _auditService.Object);

        await sut.Handle(new BatchDeleteQueueMessagesCommand()
        {
            Ids = _messageIds
        });

        deletedBatches.Count.Should().Be(2);
        deletedBatches.Select(x => x.Count).Should().BeEquivalentTo(new[] { 20, 10 });
    }

    [Test]
    public async Task ThenWillCallAuditServiceToRecordMessageIds()
    {
        _messageIds = new List<string>();
        var i = 0;
        while (i++ < 10)
        {
            _messageIds.Add($"id{i}");
        }

        _cosmosDbContext = new Mock<ICosmosMessageDbContext>(MockBehavior.Strict);
        _cosmosDbContext.Setup(x => x.DeleteQueueMessagesAsync(It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);
        _auditService.Setup(x => x.WriteAudit(It.IsAny<MessageQueueDeleteAuditMessage>()));

        var sut = new BatchDeleteQueueMessagesCommandHandler(_cosmosDbContext.Object, _serviceBusSettings, _logger.Object, _auditService.Object);

        await sut.Handle(new BatchDeleteQueueMessagesCommand()
        {
            Ids = _messageIds
        });

        _auditService.Verify(x => x.WriteAudit(It.IsAny<MessageQueueDeleteAuditMessage>()), Times.Once);
    }
}