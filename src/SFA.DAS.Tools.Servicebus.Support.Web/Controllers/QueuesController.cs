using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessageCountPerUser;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using static SFA.DAS.Tools.Servicebus.Support.Web.Models.QueueInformationModel;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers;

public class QueuesController(
    IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse> getQueuesQuery,
    IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse> getMessageCountPerUser,
    IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse> getUserSessionsQuery)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string sort, string order, string search, int offset, int limit, bool filterEmptyQueues)
    {
        var queuesResponse = await getQueuesQuery.Handle(new GetQueuesQuery());

        var messageCountResponse = await getMessageCountPerUser.Handle(new GetMessageCountPerUserQuery());

        var userSessionResponse = await getUserSessionsQuery.Handle(new GetUserSessionsQuery());
        var userSessions = userSessionResponse.UserSessions.ToList();


        var returnedQueues = queuesResponse.Queues;

        if (filterEmptyQueues)
        {
            returnedQueues = returnedQueues.Where(s => s.MessageCount > 0
                                                       || (messageCountResponse.QueueMessageCount.ContainsKey(s.Name) && messageCountResponse.QueueMessageCount[s.Name].Count > 0));
        }

        return Json(new QueueInformationModel
        {
            Total = returnedQueues.Count(),
            Rows = returnedQueues.Select(q => new QueueCountInfo
            {
                Id = q.Name,
                Name = q.Name,
                MessageCount = q.MessageCount,
                MessageCountInvestigation = messageCountResponse.QueueMessageCount.ContainsKey(q.Name) ? FormatMessageForUnderInvestigationCount(messageCountResponse.QueueMessageCount[q.Name], userSessions) : "0"
            })
        });
    }

    private string FormatMessageForUnderInvestigationCount(List<UserMessageCount> userMessageCounts, List<UserSession> userSessions)
    {
        var totalCount = userMessageCounts.Sum(c => c.MessageCount);
        var names = new List<string>();

        foreach (var msgCount in userMessageCounts)
        {
            var userName = userSessions.FirstOrDefault(s => s.UserId == msgCount.UserId)?.UserName;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                names.Add(userName);
            }
        }

        if (names.Count == 0 && totalCount > 0)
        {
            return $"{totalCount} (stale ownership)";
        }

        var msg = new StringBuilder();
        msg.Append(totalCount);
        if (names.Any())
        {
            msg.Append(" (");
            msg.Append(string.Join(",", names));
            msg.Append(")");
        }

        return msg.ToString();
    }
}