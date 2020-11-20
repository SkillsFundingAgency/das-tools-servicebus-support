using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessageCountPerUser;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Web.Controllers;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.UnitTests.Controllers
{
    public class QueuesControllerTest
    {
        private Mock<IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse>> getQueuesQuery;
        private Mock<IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse>> getMessageCountPerUser;
        private Mock<IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse>> getUserSessionsQuery;

        private GetQueuesQueryResponse getQueuesQueryResponse = new GetQueuesQueryResponse
        {
            Queues = new List<QueueInfo>
                {
                    new QueueInfo
                    {
                        Name = "TestQueue1",
                        MessageCount = 10
                    },
                    new QueueInfo
                    {
                        Name = "TestQueue2",
                        MessageCount = 20
                    },
                    new QueueInfo
                    {
                        Name = "TestQueue3",
                        MessageCount = 0
                    },
                    new QueueInfo
                    {
                        Name = "TestQueue4",
                        MessageCount = 0
                    },
                }
        };

        private GetMessageCountPerUserQueryResponse getMessageCountPerUserQueryResponse = new GetMessageCountPerUserQueryResponse
        {
            QueueMessageCount = new Dictionary<string, List<Domain.UserMessageCount>>
            {
                {"TestQueue1", new List<Domain.UserMessageCount>
                    {
                        new Domain.UserMessageCount { MessageCount = 20, Queue = "TestQueue1", UserId = "UserA" },
                        new Domain.UserMessageCount { MessageCount = 40, Queue = "TestQueue1", UserId = "UserB" }
                    }
                },
                {"TestQueue2", new List<Domain.UserMessageCount>{ new Domain.UserMessageCount { MessageCount = 20, Queue = "TestQueue4", UserId = "UserC" } } },
                {"TestQueue3", new List<Domain.UserMessageCount>{ new Domain.UserMessageCount { MessageCount = 30, Queue = "TestQueue2", UserId = "UserD" } } }
            }
        };

        private GetUserSessionsQueryResponse getUserSessionQueryResponse = new GetUserSessionsQueryResponse
        {
            UserSessions = new List<UserSession>
            {
                new UserSession{ UserId = "UserA", UserName="UserA_Username"  },
                new UserSession{ UserId = "UserB", UserName="UserB_Username" },
                new UserSession{ UserId = "UserC", UserName="UserC_Username" },
                new UserSession{ UserId = "UserD", UserName="UserD_Username" },
            }
        };

        public QueuesControllerTest()
        {
            getQueuesQuery = new Mock<IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse>>();
            getMessageCountPerUser = new Mock<IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse>>();
            getUserSessionsQuery = new Mock<IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse>>();

            getQueuesQuery.Setup(s => s.Handle(It.IsAny<GetQueuesQuery>())).Returns(Task.FromResult(getQueuesQueryResponse));
            getMessageCountPerUser.Setup(s => s.Handle(It.IsAny<GetMessageCountPerUserQuery>())).Returns(Task.FromResult(getMessageCountPerUserQueryResponse));
            getUserSessionsQuery.Setup(s => s.Handle(It.IsAny<GetUserSessionsQuery>())).Returns(Task.FromResult(getUserSessionQueryResponse));

        }

        [Test]
        public async Task WhenCallingIndexWithNoFilteringReturnDataIndicatingQueueCounts()
        {
            //Given
            var sut = new QueuesController(getQueuesQuery.Object, getMessageCountPerUser.Object, getUserSessionsQuery.Object);

            //When
            var result = await sut.Index(string.Empty, string.Empty, string.Empty, 0, 0, false);

            //Then
            var queueInfoModel = result.Should().BeOfType<JsonResult>().Which.Value.Should().BeOfType<QueueInformationModel>().Which;

            queueInfoModel.Total.Should().Be(4);
            queueInfoModel.Rows.Should().BeEquivalentTo(
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue1",
                    MessageCount = 10,
                    MessageCountInvestigation = "60 (UserA_Username,UserB_Username)",
                    Name = "TestQueue1"
                },
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue2",
                    MessageCount = 20,
                    MessageCountInvestigation = "20 (UserC_Username)",
                    Name = "TestQueue2"
                },
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue3",
                    MessageCount = 0,
                    MessageCountInvestigation = "30 (UserD_Username)",
                    Name = "TestQueue3"
                },
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue4",
                    MessageCount = 0,
                    MessageCountInvestigation = "0",
                    Name = "TestQueue4"
                });
        }

        [Test]
        public async Task WhenCallingIndexWithFilteringEmptyQueuesReturnDataIndicatingQueueCounts()
        {
            //Given
            var sut = new QueuesController(getQueuesQuery.Object, getMessageCountPerUser.Object, getUserSessionsQuery.Object);

            //When
            var result = await sut.Index(string.Empty, string.Empty, string.Empty, 0, 0, true);

            //Then
            var queueInfoModel = result.Should().BeOfType<JsonResult>().Which.Value.Should().BeOfType<QueueInformationModel>().Which;

            queueInfoModel.Total.Should().Be(3);
            queueInfoModel.Rows.Should().BeEquivalentTo(
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue1",
                    MessageCount = 10,
                    MessageCountInvestigation = "60 (UserA_Username,UserB_Username)",
                    Name = "TestQueue1"
                },
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue2",
                    MessageCount = 20,
                    MessageCountInvestigation = "20 (UserC_Username)",
                    Name = "TestQueue2"
                },
                new QueueInformationModel.QueueCountInfo
                {
                    Id = "TestQueue3",
                    MessageCount = 0,
                    MessageCountInvestigation = "30 (UserD_Username)",
                    Name = "TestQueue3"
                });
        }

    }
}
