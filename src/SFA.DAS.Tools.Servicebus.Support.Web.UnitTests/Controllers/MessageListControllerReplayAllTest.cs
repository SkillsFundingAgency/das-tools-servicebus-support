using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.Controllers;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.UnitTests.Controllers;

public class MessageListControllerReplayAllTest
{
    private const string UserId = "user-1";
    private const string ErrorQueue = "sfa.das.commitmentsv2.externalhandlers-errors";
    private const string ProcessingQueue = "sfa.das.commitmentsv2.externalhandlers";

    private Mock<IUserService> userService;
    private Mock<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>> getMessagesQuery;
    private Mock<IMessageService> messageService;
    private MessageListController controller;

    [SetUp]
    public void SetUp()
    {
        userService = new Mock<IUserService>();
        userService.Setup(x => x.GetUserId()).Returns(UserId);

        getMessagesQuery = new Mock<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>>();
        messageService = new Mock<IMessageService>();

        controller = new MessageListController(
            userService.Object,
            getMessagesQuery.Object,
            null,
            null,
            messageService.Object,
            null,
            Options.Create(new ServiceBusErrorManagementSettings
            {
                ErrorQueueRegex = "-errors$"
            }),
            null,
            null,
            null,
            null,
            Mock.Of<ILogger<MessageListController>>());
    }

    [Test]
    public async Task ThenItReplaysTheNextBatchForTheCurrentUserToTheProcessingQueue()
    {
        var messages = new List<QueueMessage>
        {
            new QueueMessage { Id = "1" },
            new QueueMessage { Id = "2" }
        };

        getMessagesQuery
            .Setup(x => x.Handle(It.IsAny<GetMessagesQuery>()))
            .ReturnsAsync(new GetMessagesQueryResponse
            {
                Messages = messages,
                UnfilteredCount = 250,
                Count = 250
            });

        var result = await controller.ReplayAllToProcessingQueue(ErrorQueue) as JsonResult;

        getMessagesQuery.Verify(x => x.Handle(It.Is<GetMessagesQuery>(q =>
            q.UserId == UserId &&
            q.SearchProperties.Offset == 0 &&
            q.SearchProperties.Limit == MessageListController.ReplayAllBatchSize)), Times.Once);

        messageService.Verify(x => x.ReplayMessages(
            It.Is<IEnumerable<QueueMessage>>(m => m.Select(msg => msg.Id).SequenceEqual(new[] { "1", "2" })),
            ProcessingQueue), Times.Once);

        var body = result.Value.Should().BeOfType<ReplayAllBatchResponse>().Subject;
        body.Processed.Should().Be(2);
        body.Remaining.Should().Be(248);
        body.Done.Should().BeFalse();
        body.Error.Should().BeNull();
    }

    [Test]
    public async Task ThenAnEmptyCheckoutDoesNotSend()
    {
        getMessagesQuery
            .Setup(x => x.Handle(It.IsAny<GetMessagesQuery>()))
            .ReturnsAsync(new GetMessagesQueryResponse
            {
                Messages = new List<QueueMessage>(),
                UnfilteredCount = 0,
                Count = 0
            });

        var result = await controller.ReplayAllToProcessingQueue(ErrorQueue) as JsonResult;

        messageService.Verify(
            x => x.ReplayMessages(It.IsAny<IEnumerable<QueueMessage>>(), It.IsAny<string>()),
            Times.Never);

        var body = result.Value.Should().BeOfType<ReplayAllBatchResponse>().Subject;
        body.Processed.Should().Be(0);
        body.Remaining.Should().Be(0);
        body.Done.Should().BeTrue();
    }
}
