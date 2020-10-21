using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessageCountPerUser;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class QueuesController : Controller
    {        
        private readonly IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse> _getQueuesQuery;
        private readonly IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse> _getMessageCountPerUser;
        private readonly IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse> _getUserSessionsQuery;

        public QueuesController(            
            IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse> getQueuesQuery,
            IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse> getMessageCountPerUser,
            IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse> getUserSessionsQuery)
        {
            _getQueuesQuery = getQueuesQuery;
            _getMessageCountPerUser = getMessageCountPerUser;
            _getUserSessionsQuery = getUserSessionsQuery;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sort, string order, string search, int offset, int limit)
        {
            var queuesResponse = await _getQueuesQuery.Handle(new GetQueuesQuery());

            var messageCountResponse = await _getMessageCountPerUser.Handle(new GetMessageCountPerUserQuery());

            var userSessionResponse = await _getUserSessionsQuery.Handle(new GetUserSessionsQuery());
            var userSessions = userSessionResponse.UserSessions.ToList();

            return Json(new
            {
                Total = queuesResponse.Queues.Count(),
                Rows = queuesResponse.Queues.Select(q => new
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
            var msg = new StringBuilder();
            msg.Append(userMessageCounts.Sum(c => c.MessageCount));

            var names = new List<string>();
            foreach (var msgCount in userMessageCounts)
            {
                names.Add(userSessions.FirstOrDefault(s => s.UserId == msgCount.UserId).UserName);
            }

            msg.Append(" (");
            msg.Append(string.Join(",", names));
            msg.Append(")");

            return msg.ToString();
        }
    }
}
